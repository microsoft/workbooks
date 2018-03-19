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

using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.CodeAnalysis.Roslyn;
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
            IWorkspaceService compilationWorkspace,
            XCB.ScriptContext context,
            Func<string, SourceText> getSourceTextByModelId)
        {
            this.context = context
                ?? throw new ArgumentNullException (nameof (context));

            this.getSourceTextByModelId = getSourceTextByModelId
                ?? throw new ArgumentNullException (nameof (getSourceTextByModelId));

            controller = new CompletionController ((RoslynCompilationWorkspace)compilationWorkspace);

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
            Position position,
            CancellationToken cancellationToken)
        {
            var sourceTextContent = getSourceTextByModelId (modelId);
            var completionTask = controller.ProvideFilteredCompletionItemsAsync (
                sourceTextContent,
                position,
                cancellationToken);

            return context.ToMonacoPromise (
                completionTask,
                ToMonacoCompletionItems,
                MainThread.TaskScheduler,
                raiseErrors: false);
        }

        static dynamic ToMonacoCompletionItems (XCB.ScriptContext context, IEnumerable<CompletionItem> items)
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
