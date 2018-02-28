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

using Xamarin.Interactive.CodeAnalysis.SignatureHelp;
using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis.Monaco
{
    sealed class SignatureHelpProvider : IDisposable
    {
        const string TAG = nameof (SignatureHelpProvider);

        readonly ScriptContext context;
        readonly Func<string, SourceText> getSourceTextByModelId;
        readonly SignatureHelpController controller;

        #pragma warning disable 0414
        readonly dynamic providerTicket;
        #pragma warning restore 0414

        public SignatureHelpProvider (
            RoslynCompilationWorkspace compilationWorkspace,
            ScriptContext context,
            Func<string, SourceText> getSourceTextByModelId)
        {
            this.context = context
                ?? throw new ArgumentNullException (nameof (context));

            this.getSourceTextByModelId = getSourceTextByModelId
                ?? throw new ArgumentNullException (nameof (getSourceTextByModelId));

            controller = new SignatureHelpController (compilationWorkspace);

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
            string modelId,
            LinePosition linePosition,
            CancellationToken cancellationToken)
        {
            var sourceTextContent = getSourceTextByModelId (modelId);

            var computeTask = controller.ComputeSignatureHelpAsync (
                sourceTextContent,
                linePosition,
                cancellationToken);

            return context.ToMonacoPromise (
                computeTask,
                ToMonacoSignatureHelp,
                MainThread.TaskScheduler,
                raiseErrors: false);
        }

        static dynamic ToMonacoSignatureHelp (ScriptContext context, SignatureHelpViewModel signatureHelp)
        {
            if (!(signatureHelp.Signatures?.Length > 0))
                return null;

            dynamic signatures = context.CreateArray ();

            foreach (var sig in signatureHelp.Signatures) {
                dynamic parameters = context.CreateArray ();

                if (sig.Parameters?.Length > 0)
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