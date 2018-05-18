//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client.AgentProcesses;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

[assembly: AgentProcessRegistration (
    "webassembly-monowebassembly",
    typeof (WebAssemblyAgentProcess))]

namespace Xamarin.Interactive.Client.AgentProcesses
{
    sealed class WebAssemblyAgentProcess : IAgentProcess
    {
        const string TAG = nameof (WebAssemblyAgentProcess);

        public IWorkbookAppInstallation WorkbookApp { get; }

        public event EventHandler UnexpectedlyTerminated;

        public WebAssemblyAgentProcess (IWorkbookAppInstallation workbookApp)
            => WorkbookApp = workbookApp ?? throw new ArgumentNullException (nameof (workbookApp));

        public Task StartAgentProcessAsync (
            IdentifyAgentRequest identifyAgentRequest,
            IMessageService messageService,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task TerminateAgentProcessAsync ()
            => Task.CompletedTask;
    }
}