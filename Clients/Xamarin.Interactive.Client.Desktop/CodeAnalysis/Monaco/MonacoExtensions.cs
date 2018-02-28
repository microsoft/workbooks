//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
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
    }
}