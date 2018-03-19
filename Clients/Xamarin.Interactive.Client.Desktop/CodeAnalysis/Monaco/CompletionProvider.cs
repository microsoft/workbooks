//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.CodeAnalysis.Models;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
    sealed class CompletionProvider : IDisposable
    {
        const string TAG = nameof (CompletionProvider);

        readonly IWorkspaceService workspace;
        readonly ScriptContext context;
        readonly Func<string, CodeCellId> monacoModelIdToCodeCellIdMapper;

        #pragma warning disable 0414
        readonly dynamic providerTicket;
        #pragma warning restore 0414

        public CompletionProvider (
            IWorkspaceService workspace,
            ScriptContext context,
            Func<string, CodeCellId> monacoModelIdToCodeCellIdMapper)
        {
            this.workspace = workspace
                ?? throw new ArgumentNullException (nameof (workspace));

            this.context = context
                ?? throw new ArgumentNullException (nameof (context));

            this.monacoModelIdToCodeCellIdMapper = monacoModelIdToCodeCellIdMapper
                ?? throw new ArgumentNullException (nameof (monacoModelIdToCodeCellIdMapper));

            providerTicket = context.GlobalObject.xiexports.monaco.RegisterWorkbookCompletionItemProvider (
                "csharp",
                (ScriptFunc)ProvideCompletionItems);
        }

        public void Dispose ()
            => providerTicket.dispose ();

        dynamic ProvideCompletionItems (dynamic self, dynamic args)
            => ProvideCompletionItems (
                args [0].id.ToString (),
                MonacoExtensions.FromMonacoPosition (args [1]),
                MonacoExtensions.FromMonacoCancellationToken (args [2]));

        object ProvideCompletionItems (
            string monacoModelId,
            Position position,
            CancellationToken cancellationToken)
            => context.ToMonacoPromise (
                workspace.GetCompletionsAsync (
                    monacoModelIdToCodeCellIdMapper (monacoModelId),
                    position,
                    cancellationToken),
                ToMonacoCompletionItems,
                MainThread.TaskScheduler,
                raiseErrors: false);

        static dynamic ToMonacoCompletionItems (ScriptContext context, IEnumerable<CompletionItem> items)
        {
            dynamic arr = context.CreateArray ();

            foreach (var item in items) {
                arr.push (context.CreateObject (o => {
                    o.label = item.Label;
                    if (item.Detail != null)
                        o.detail = item.Detail;
                    if (item.InsertText != null)
                        o.insertText = item.InsertText;
                    o.kind = (int)item.Kind;
                }));
            }

            return arr;
        }
    }
}