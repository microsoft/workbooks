//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Session;

using Xamarin.Interactive.Client.Web.WebAssembly;
using Xamarin.Interactive.Client.AgentProcesses;

[assembly: AgentTicketRegistration(
    "webassembly-monowebassembly",
    typeof (WebAssemblyAgentTicket))]

namespace Xamarin.Interactive.Client.Web.WebAssembly
{
    sealed class WebAssemblyAgentTicket : IAgentTicket
    {
        readonly IAgentProcessManager processManager;
        readonly Action disconnectedHandler;
        readonly IMessageService messageService;
        readonly IAgentClient client;

        public IMessageService MessageService => messageService;

        public bool IsDisposed { get; private set; }

        public event EventHandler Disposed;

        public WebAssemblyAgentTicket (
            IAgentProcessManager processManager,
            IMessageService messageService,
            Action disconnectedHandler)
        {
            // WASM doesn't currently use these, but we may someday.
            this.processManager = processManager;
            this.disconnectedHandler = disconnectedHandler;
            this.messageService = messageService;

            client = new WebAssemblyAgentClient ();
        }

        public void Dispose()
        {
            if (!IsDisposed) {
                IsDisposed = true;
                Disposed?.Invoke (this, EventArgs.Empty);
            }
        }

        public Task<AgentIdentity> GetAgentIdentityAsync (CancellationToken cancellationToken = default)
        {
            var wasmWorkbookApp = WorkbookAppInstallation.LookupById ("webassembly-monowebassembly");
            return Task.FromResult (
                new AgentIdentity (
                    AgentType.Console,
                    wasmWorkbookApp.Sdk,
                    "Xamarin Workbooks (WASM)"));
        }

        public Task<IAgentClient> GetClientAsync (CancellationToken cancellationToken = default)
            => Task.FromResult (client);
    }
}