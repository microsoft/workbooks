//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    interface IAgentProcessManager
    {
        Task<IAgentProcessState> StartAsync (
            AgentProcessTicket ticket,
            CancellationToken cancellationToken = default (CancellationToken));

        void Terminate ();
    }
}
