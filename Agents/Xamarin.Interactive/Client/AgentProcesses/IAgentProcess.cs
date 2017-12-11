//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    interface IAgentProcess
    {
        event EventHandler UnexpectedlyTerminated;

        IWorkbookAppInstallation WorkbookApp { get; }

        Task StartAgentProcessAsync (
            IdentifyAgentRequest identifyAgentRequest,
            IMessageService messageService,
            CancellationToken cancellationToken);

        Task TerminateAgentProcessAsync ();
    }
}