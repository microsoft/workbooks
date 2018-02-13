//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

using Xamarin.CrossBrowser;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
    static class MonacoExtensions
    {
        const string TAG = nameof (MonacoExtensions);

        static readonly SymbolDisplayFormat symbolDisplayFormat = SymbolDisplayFormat.CSharpErrorMessageFormat
            .WithParameterOptions (
                SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeParamsRefOut)
            .WithMemberOptions (
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeContainingType |
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeRef |
                SymbolDisplayMemberOptions.IncludeExplicitInterface);

        public static CancellationToken FromMonacoCancellationToken (dynamic token)
        {
            var cts = new CancellationTokenSource ();
            token.onCancellationRequested ((ScriptAction)((self, args) => {
                cts.Cancel ();
            }));

            if ((bool)token.isCancellationRequested)
                cts.Cancel ();

            return cts.Token;
        }

        public static dynamic ToMonacoPromise<TResult> (
            this ScriptContext context,
            Task<TResult> task,
            Func<ScriptContext, TResult, object> converter,
            TaskScheduler continuationScheduler,
            bool logError = true,
            bool raiseErrors = true)
        {
            return context.GlobalObject.xiexports.monaco.Promise ((ScriptAction)((self, args) => {
                var complete = args [0];
                var error = args [1];

                task.ContinueWith (t => {
                    // There doesn't appear to be any way to notify the promise that the
                    // task was canceled. The assumption appears to be that cancellation
                    // originated with a JS CancellationTokenSource (which we do not get
                    // access to). So...just doing nothing on cancellation.
                    if (t.IsFaulted) {
                        if (logError)
                            Logging.Log.Error (TAG, t.Exception);
                        if (raiseErrors)
                            error (context.GlobalObject.xiexports.createError (t.Exception?.Message));
                    } else if (t.IsCompleted && !t.IsCanceled) {
                        try {
                            complete (converter (context, t.Result));
                        } catch (Exception e) {
                            if (logError)
                                Logging.Log.Error (TAG, e);
                            if (raiseErrors)
                                error (context.GlobalObject.xiexports.createError (e.Message));
                        }
                    }
                }, continuationScheduler);
            }));
        }

        public static dynamic ToMonacoRange (this ScriptContext context, LinePositionSpan span) =>
            context.CreateObject (o => {
                o.startLineNumber = span.Start.Line + 1;
                o.startColumn = span.Start.Character + 1;
                o.endLineNumber = span.End.Line + 1;
                o.endColumn = span.End.Character + 1;
            });

        public static dynamic ToMonacoPosition (this ScriptContext context, LinePosition position) =>
            context.CreateObject (o => {
                o.lineNumber = position.Line + 1;
                o.column = position.Character + 1;
            });

        public static LinePosition FromMonacoPosition (dynamic position) =>
            new LinePosition ((int)position.lineNumber - 1, (int)position.column - 1);

        public static string ToMonacoSignatureString (this ISymbol symbol) =>
            symbol.ToDisplayString (symbolDisplayFormat);

        public static int ToMonacoCompletionItemKind (ImmutableArray<string> completionTags)
        {
            const CompletionItemKind defaultKind = CompletionItemKind.Text;

            if (completionTags.Length == 0)
                return (int)defaultKind;

            if (completionTags.Contains (CompletionTags.Assembly)
                || completionTags.Contains (CompletionTags.Namespace)
                || completionTags.Contains (CompletionTags.Project)
                || completionTags.Contains (CompletionTags.Module))
                return (int)CompletionItemKind.Module;

            if (completionTags.Contains (CompletionTags.Class)
                || completionTags.Contains (CompletionTags.Structure))
                return (int)CompletionItemKind.Class;

            if (completionTags.Contains (CompletionTags.Constant)
                || completionTags.Contains (CompletionTags.Field)
                || completionTags.Contains (CompletionTags.Delegate)
                || completionTags.Contains (CompletionTags.Event)
                || completionTags.Contains (CompletionTags.Local))
                return (int)CompletionItemKind.Field;

            if (completionTags.Contains (CompletionTags.Enum)
                || completionTags.Contains (CompletionTags.EnumMember))
                return (int)CompletionItemKind.Enum;

            if (completionTags.Contains (CompletionTags.Method)
                || completionTags.Contains (CompletionTags.Operator))
                return (int)CompletionItemKind.Method;

            if (completionTags.Contains (CompletionTags.ExtensionMethod))
                return (int)CompletionItemKind.Function;

            if (completionTags.Contains (CompletionTags.Interface))
                return (int)CompletionItemKind.Interface;

            if (completionTags.Contains (CompletionTags.Property))
                return (int)CompletionItemKind.Property;

            if (completionTags.Contains (CompletionTags.Keyword))
                return (int)CompletionItemKind.Keyword;

            if (completionTags.Contains (CompletionTags.Reference))
                return (int)CompletionItemKind.Reference;

            if (completionTags.Contains (CompletionTags.Snippet))
                return (int)CompletionItemKind.Snippet;

            return (int)defaultKind;
        }

        enum CompletionItemKind
        {
            Text = 0,
            Method = 1,
            Function = 2,
            Constructor = 3,
            Field = 4,
            Variable = 5,
            Class = 6,
            Interface = 7,
            Module = 8,
            Property = 9,
            Unit = 10,
            Value = 11,
            Enum = 12,
            Keyword = 13,
            Snippet = 14,
            Color = 15,
            File = 16,
            Reference = 17,
        }
    }
}