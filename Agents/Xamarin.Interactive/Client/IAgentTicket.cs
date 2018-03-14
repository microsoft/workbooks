//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client
{
    interface IAgentTicket : INotifyDisposed
    {
        IReadOnlyList<string> AssemblySearchPaths { get; }

        IMessageService MessageService { get; }

        Task<AgentIdentity> GetAgentIdentityAsync (
            CancellationToken cancellationToken = default (CancellationToken));

        Task<AgentClient> GetClientAsync (
            CancellationToken cancellationToken = default (CancellationToken));
    }
}