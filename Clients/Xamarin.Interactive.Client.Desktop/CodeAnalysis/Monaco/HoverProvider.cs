//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.CodeAnalysis.Models;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
    sealed class HoverProvider : IDisposable
    {
        const string TAG = nameof (HoverProvider);

        readonly IWorkspaceService workspace;
        readonly ScriptContext context;
        readonly Func<string, CodeCellId> monacoModelIdToCodeCellIdMapper;

        #pragma warning disable 0414
        readonly dynamic providerTicket;
        #pragma warning restore 0414

        public HoverProvider (
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
            string monacoModelId,
            Position position,
            CancellationToken cancellationToken)
            => context.ToMonacoPromise (
                workspace.GetHoverAsync (
                    monacoModelIdToCodeCellIdMapper (monacoModelId),
                    position,
                    cancellationToken),
                ToMonacoHover,
                MainThread.TaskScheduler,
                raiseErrors: false);

        static dynamic ToMonacoHover (ScriptContext context, Hover hover)
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