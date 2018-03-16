//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis.Text;

using XCB = Xamarin.CrossBrowser;

using Xamarin.Interactive.CodeAnalysis.Completion;
using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
    sealed class CompletionProvider : IDisposable
    {
        const string TAG = nameof (CompletionProvider);

        readonly XCB.ScriptContext context;
        readonly Func<string, SourceText> getSourceTextByModelId;
        readonly CompletionController controller;

        #pragma warning disable 0414
        readonly dynamic providerTicket;
        #pragma warning restore 0414

        public CompletionProvider (
            RoslynCompilationWorkspace compilationWorkspace,
            XCB.ScriptContext context,
            Func<string, SourceText> getSourceTextByModelId)
        {
            this.context = context
                ?? throw new ArgumentNullException (nameof (context));

            this.getSourceTextByModelId = getSourceTextByModelId
                ?? throw new ArgumentNullException (nameof (getSourceTextByModelId));

            controller = new CompletionController (compilationWorkspace);

            providerTicket = context.GlobalObject.xiexports.monaco.RegisterWorkbookCompletionItemProvider (
                "csharp",
                (XCB.ScriptFunc)ProvideCompletionItems);
        }

        public void Dispose ()
        {
            providerTicket.dispose ();
        }

        dynamic ProvideCompletionItems (dynamic self, dynamic args)
            => ProvideCompletionItems (
                args [0].id.ToString (),
                MonacoExtensions.FromMonacoPosition (args [1]),
                MonacoExtensions.FromMonacoCancellationToken (args [2]));

        object ProvideCompletionItems (
            string modelId,
            LinePosition linePosition,
            CancellationToken cancellationToken)
        {
            var sourceTextContent = getSourceTextByModelId (modelId);
            var completionTask = controller.ProvideFilteredCompletionItemsAsync (
                sourceTextContent,
                linePosition,
                cancellationToken);

            return context.ToMonacoPromise (
                completionTask,
                ToMonacoCompletionItems,
                MainThread.TaskScheduler,
                raiseErrors: false);
        }

        static dynamic ToMonacoCompletionItems (XCB.ScriptContext context, IEnumerable<CompletionItemViewModel> items)
        {
            dynamic arr = context.CreateArray ();

            foreach (var item in items) {
                arr.push (context.CreateObject (o => {
                    o.label = item.DisplayText;
                    if (item.ItemDetail != null)
                        o.detail = item.ItemDetail;
                    if (item.InsertionText != null)
                        o.insertText = item.InsertionText;
                    o.kind = Client.Monaco.MonacoExtensions.ToMonacoCompletionItemKind (item.CompletionItem.Tags);
                }));
            }

            return arr;
        }
    }
}
