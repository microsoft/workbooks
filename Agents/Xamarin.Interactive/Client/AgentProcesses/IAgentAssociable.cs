//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    interface IAgentAssociable
    {
        Task<AgentIdentity> GetAgentAssociationAsync (
            AgentIdentity agentIdentity,
            CancellationToken cancellationToken);
    }
}