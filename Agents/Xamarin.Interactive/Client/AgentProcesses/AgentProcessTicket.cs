//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    sealed class AgentProcessTicket : IAgentTicket
    {
        static int lastId;

        readonly Action disconnectedHandler;

        public IAgentProcessManager AgentProcessManager { get; }
        public IMessageService MessageService { get; }

        public int Id { get; }

        public IReadOnlyList<string> AssemblySearchPaths { get; private set; }
        public bool IsDisposed { get; private set; }

        public event EventHandler Disposed;

        public AgentProcessTicket (
            IAgentProcessManager agentProcessManager,
            IMessageService messageService,
            Action disconnectedHandler)
        {
            AgentProcessManager = agentProcessManager
                ?? throw new ArgumentNullException (nameof (agentProcessManager));

            MessageService = messageService
                ?? throw new ArgumentNullException (nameof (disconnectedHandler));

            this.disconnectedHandler = disconnectedHandler;

            Id = Interlocked.Increment (ref lastId);
        }

        public void Dispose ()
        {
            if (!IsDisposed) {
                IsDisposed = true;
                Disposed?.Invoke (this, EventArgs.Empty);
            }
        }

        public async Task<IAgentProcessState> GetAgentProcessStateAsync (CancellationToken cancellationToken)
        {
            var agentProcessState = await AgentProcessManager.StartAsync (
                ticket: this,
                cancellationToken: cancellationToken);

            AssemblySearchPaths = agentProcessState
                .AgentProcess
                .WorkbookApp
                .Sdk
                .AssemblySearchPaths;

            return agentProcessState;
        }

        public async Task<AgentIdentity> GetAgentIdentityAsync (
            CancellationToken cancellationToken = default (CancellationToken))
            => (await GetAgentProcessStateAsync (cancellationToken)).AgentIdentity;

        public async Task<AgentClient> GetClientAsync (
            CancellationToken cancellationToken = default(CancellationToken))
            => (await GetAgentProcessStateAsync (cancellationToken)).AgentClient;

        public void NotifyDisconnected ()
            => MainThread.Post (disconnectedHandler);
    }
}