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
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    sealed class AgentProcessManager : IAgentProcessManager
    {
        sealed class AgentProcessState : IAgentProcessState
        {
            public IAgentProcess AgentProcess { get; }

            public AgentIdentity AgentIdentity { get; set; }
            public AgentClient AgentClient { get; set; }

            public AgentProcessState (IWorkbookAppInstallation workbookApp, Type agentProcessType)
                => AgentProcess = (IAgentProcess)Activator.CreateInstance (
                    agentProcessType,
                    workbookApp);
        }

        readonly string TAG;

        readonly List<AgentProcessTicket> tickets = new List<AgentProcessTicket> ();
        readonly SemaphoreSlim startWait = new SemaphoreSlim (1, 1);

        readonly IWorkbookAppInstallation workbookApp;
        readonly Type agentProcessType;

        readonly object runningAgentProcessStateLock = new object ();
        AgentProcessState runningAgentProcessState;

        public AgentProcessManager (IWorkbookAppInstallation workbookApp, Type agentProcessType)
        {
            if (!typeof (IAgentProcess).IsAssignableFrom (agentProcessType))
                throw new ArgumentException ("Must be an IAgentProcess", nameof (agentProcessType));

            TAG = $"{nameof (AgentProcessManager)}<{agentProcessType.Name}>";

            this.workbookApp = workbookApp;
            this.agentProcessType = agentProcessType;
        }

        void HandleTicketDisposed (object sender, EventArgs args)
        {
            var ticket = sender as AgentProcessTicket;
            if (ticket == null)
                return;

            ticket.Disposed -= HandleTicketDisposed;

            bool terminate;

            lock (tickets) {
                tickets.Remove (ticket);
                terminate = tickets.Count == 0;
            }

            if (terminate)
                Terminate ();
        }

        void HandleAgentProcessUnexpectedlyTerminated (object sender, EventArgs e)
        {
            lock (runningAgentProcessStateLock) {
                if (sender == runningAgentProcessState?.AgentProcess)
                    Terminate (runningAgentProcessState);
            }
        }

        public void Terminate ()
        {
            lock (runningAgentProcessStateLock) {
                if (runningAgentProcessState != null)
                    Terminate (runningAgentProcessState);
            }
        }

        void Terminate (AgentProcessState agentProcessState)
        {
            lock (runningAgentProcessStateLock) {
                agentProcessState.AgentProcess.UnexpectedlyTerminated
                    -= HandleAgentProcessUnexpectedlyTerminated;

                if (runningAgentProcessState == agentProcessState)
                    runningAgentProcessState = null;
            }

            AgentProcessTicket [] copiedTickets;
            lock (tickets) {
                copiedTickets = tickets.ToArray ();
                tickets.Clear ();
            }

            MainThread.Post (() => {
                foreach (var ticket in copiedTickets) {
                    try {
                        ticket.NotifyDisconnected ();
                    } catch (Exception e) {
                        Log.Error (TAG, "exception when notifying ticket of disconnect", e);
                    }
                }
            });

            // Always run this as a form of disposal, because new AgentProcesses are created on-demand.
            // AgentProcess implementations should not let their Terminate implementations throw.
            agentProcessState.AgentProcess.TerminateAgentProcessAsync ().Forget ();
        }

        void RegisterTicket (AgentProcessTicket ticket)
        {
            if (ticket.IsDisposed)
                throw new ObjectDisposedException (nameof (ticket));

            lock (tickets) {
                if (!tickets.Contains (ticket)) {
                    tickets.Add (ticket);
                    ticket.Disposed += HandleTicketDisposed;
                }
            }
        }

        void LogStartStatus (AgentProcessTicket ticket, string status)
            => Log.Debug (TAG, $"{nameof (StartAsync)}(ticket={ticket.Id}): {status}");

        public async Task<IAgentProcessState> StartAsync (
            AgentProcessTicket ticket,
            CancellationToken cancellationToken)
        {
            if (ticket == null)
                throw new ArgumentNullException (nameof (ticket));

            if (ticket.IsDisposed)
                throw new ObjectDisposedException (nameof (ticket));

            try {
                LogStartStatus (ticket, "waiting for all clear");
                await startWait.WaitAsync ().ConfigureAwait (false);

                AgentProcessState agentProcessState;

                lock (runningAgentProcessStateLock) {
                    if (runningAgentProcessState != null) {
                        LogStartStatus (ticket, "using existing agent process manager");
                        RegisterTicket (ticket);
                        return runningAgentProcessState;
                    }
                }

                LogStartStatus (ticket, "creating new agent process manager");
                agentProcessState = new AgentProcessState (workbookApp, agentProcessType);
                agentProcessState.AgentProcess.UnexpectedlyTerminated
                    += HandleAgentProcessUnexpectedlyTerminated;

                var identifyAgentRequest = ClientApp
                    .SharedInstance
                    .AgentIdentificationManager
                    .GetAgentIdentityRequest (cancellationToken);

                LogStartStatus (ticket, "waiting for agent process manager to start");
                await agentProcessState.AgentProcess.StartAgentProcessAsync (
                    identifyAgentRequest,
                    ticket.MessageService,
                    cancellationToken).ConfigureAwait (false);

                LogStartStatus (ticket, "requesting agent identity");
                agentProcessState.AgentIdentity = await ClientApp
                    .SharedInstance
                    .AgentIdentificationManager
                    .GetAgentIdentityAsync (identifyAgentRequest).ConfigureAwait (false);

                var agentClientIdentity = agentProcessState.AgentIdentity;

                if (agentProcessState.AgentProcess is IAgentAssociable agentAssociable) {
                    LogStartStatus (ticket, "waiting on agent association");
                    agentClientIdentity = await agentAssociable.GetAgentAssociationAsync (
                        agentProcessState.AgentIdentity,
                        cancellationToken).ConfigureAwait (false);
                }

                LogStartStatus (ticket, "creating agent client");
                agentProcessState.AgentClient = new AgentClient (
                    agentClientIdentity.Host,
                    agentClientIdentity.Port,
                    agentProcessState.AgentProcess.WorkbookApp.Sdk.AssemblySearchPaths);

                try {
                    LogStartStatus (ticket, "registering ticket");
                    RegisterTicket (ticket);
                } catch (ObjectDisposedException e) {
                    Terminate (agentProcessState);
                    throw e;
                }

                lock (runningAgentProcessStateLock)
                    runningAgentProcessState = agentProcessState;

                MonitorConnectionAsync (agentProcessState).Forget ();

                return agentProcessState;
            } catch (Exception e) {
                Log.Error (TAG, nameof (StartAsync), e);
                throw;
            } finally {
                startWait.Release ();
            }
        }

        async Task MonitorConnectionAsync (AgentProcessState agentProcessState)
        {
            // NOTE: we do not pass the cancellation token for the
            // ticket since the AgentClient may outlive the ticket
            try {
                await agentProcessState
                    .AgentClient
                    .OpenAgentMessageChannel (default (CancellationToken))
                    .ConfigureAwait (false);
            } finally {
                HandleAgentProcessUnexpectedlyTerminated (
                    agentProcessState?.AgentProcess, EventArgs.Empty);
            }
        }
    }
}
