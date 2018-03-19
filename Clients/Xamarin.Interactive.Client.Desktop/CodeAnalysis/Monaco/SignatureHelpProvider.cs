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
    sealed class SignatureHelpProvider : IDisposable
    {
        const string TAG = nameof (SignatureHelpProvider);

        readonly IWorkspaceService workspace;
        readonly ScriptContext context;
        readonly Func<string, CodeCellId> monacoModelIdToCodeCellIdMapper;

        #pragma warning disable 0414
        readonly dynamic providerTicket;
        #pragma warning restore 0414

        public SignatureHelpProvider (
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

            providerTicket = context.GlobalObject.xiexports.monaco.RegisterWorkbookSignatureHelpProvider (
                "csharp",
                (ScriptFunc)ProvideSignatureHelp);
        }

        public void Dispose ()
            => providerTicket.dispose ();

        dynamic ProvideSignatureHelp (dynamic self, dynamic args)
            => ProvideSignatureHelp (
                args [0].id.ToString (),
                MonacoExtensions.FromMonacoPosition (args [1]),
                MonacoExtensions.FromMonacoCancellationToken (args [2]));

        object ProvideSignatureHelp (
            string monacoModelId,
            Position position,
            CancellationToken cancellationToken)
            => context.ToMonacoPromise (
                workspace.GetSignatureHelpAsync (
                    monacoModelIdToCodeCellIdMapper (monacoModelId),
                    position,
                    cancellationToken),
                ToMonacoSignatureHelp,
                MainThread.TaskScheduler,
                raiseErrors: false);

        static dynamic ToMonacoSignatureHelp (ScriptContext context, SignatureHelp signatureHelp)
        {
            if (!(signatureHelp.Signatures?.Count > 0))
                return null;

            dynamic signatures = context.CreateArray ();

            foreach (var sig in signatureHelp.Signatures) {
                dynamic parameters = context.CreateArray ();

                if (sig.Parameters?.Count > 0)
                    foreach (var param in sig.Parameters)
                        parameters.push (context.CreateObject (o => {
                            o.label = param.Label;
                            o.documentation = param.Documentation;
                        }));

                signatures.push (context.CreateObject (o => {
                    o.label = sig.Label;
                    o.documentation = sig.Documentation;
                    o.parameters = parameters;
                }));
            }

            return context.CreateObject (o => {
                o.signatures = signatures;
                o.activeSignature = signatureHelp.ActiveSignature;
                o.activeParameter = signatureHelp.ActiveParameter;
            });
        }
    }
}