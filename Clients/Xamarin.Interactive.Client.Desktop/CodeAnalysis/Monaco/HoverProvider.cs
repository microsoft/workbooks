//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;

using Microsoft.CodeAnalysis.Text;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.CodeAnalysis.Hover;
using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
    sealed class HoverProvider : IDisposable
    {
        const string TAG = nameof (HoverProvider);

        readonly ScriptContext context;
        readonly Func<string, SourceText> getSourceTextByModelId;
        readonly HoverController controller;

        #pragma warning disable 0414
        readonly dynamic providerTicket;
        #pragma warning restore 0414

        public HoverProvider (
            RoslynCompilationWorkspace compilationWorkspace,
            ScriptContext context,
            Func<string, SourceText> getSourceTextByModelId)
        {
            this.context = context
                ?? throw new ArgumentNullException (nameof (context));

            this.getSourceTextByModelId = getSourceTextByModelId
                ?? throw new ArgumentNullException (nameof (getSourceTextByModelId));

            controller = new HoverController (compilationWorkspace);

            providerTicket = context.GlobalObject.xiexports.monaco.RegisterWorkbookHoverProvider (
                "csharp",
                (ScriptFunc)ProvideHover);
        }

        public void Dispose ()
            => providerTicket.dispose ();

        dynamic ProvideHover (dynamic self, dynamic args)
            => ProvideHover (
                args [0].id.ToString (),
                MonacoExtensions.FromMonacoPosition (args [1]),
                MonacoExtensions.FromMonacoCancellationToken (args [2]));

        object ProvideHover (
            string modelId,
            LinePosition linePosition,
            CancellationToken cancellationToken)
        {
            var sourceTextContent = getSourceTextByModelId (modelId);

            var computeTask = controller.ProvideHoverAsync (
                sourceTextContent,
                linePosition,
                cancellationToken);

            return context.ToMonacoPromise (
                computeTask,
                ToMonacoHover,
                MainThread.TaskScheduler,
                raiseErrors: false);
        }

        static dynamic ToMonacoHover (ScriptContext context, HoverViewModel hover)
        {
            if (!(hover.Contents?.Length > 0))
                return null;

            dynamic contents = context.CreateArray ();

            foreach (var content in hover.Contents)
                contents.push (content);

            return context.CreateObject (o => {
                o.contents = contents;
                o.range = context?.ToMonacoRange (hover.Range);
            });
        }
    }
}