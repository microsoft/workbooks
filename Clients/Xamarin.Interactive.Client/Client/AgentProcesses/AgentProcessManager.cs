//
// AgentProcessManager.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.AgentProcesses
{
	interface IAgentProcessManager
	{
		Task<IAgentProcessState> StartAsync (
			AgentProcessTicket ticket,
			CancellationToken cancellationToken = default (CancellationToken));

		void Terminate ();
	}

	interface IAgentProcessState
	{
		IAgentProcess AgentProcess { get; }
		AgentIdentity AgentIdentity { get; }
		AgentClient AgentClient { get; }
	}

	static class AgentProcessManager
	{
		[AttributeUsage (AttributeTargets.Assembly, AllowMultiple = true)]
		public sealed class RegistrationAttribute : Attribute
		{
			public string WorkbookAppId { get; }
			public Type ProcessManagerType { get; }

			public RegistrationAttribute (string workbookAppId, Type processManagerType)
			{
				WorkbookAppId = workbookAppId;
				ProcessManagerType = processManagerType;
			}
		}
	}

	sealed class AgentProcessManager<TAgentProcess> : IAgentProcessManager
		where TAgentProcess : class, IAgentProcess
	{
		sealed class AgentProcessState : IAgentProcessState
		{
			public TAgentProcess AgentProcess { get; }
			IAgentProcess IAgentProcessState.AgentProcess => AgentProcess;

			public AgentIdentity AgentIdentity { get; set; }
			public AgentClient AgentClient { get; set; }

			public AgentProcessState (WorkbookAppInstallation workbookApp)
				=> AgentProcess = (TAgentProcess)Activator.CreateInstance (
					typeof (TAgentProcess),
					workbookApp);
		}

		readonly string TAG;

		readonly List<AgentProcessTicket> tickets = new List<AgentProcessTicket> ();
		readonly SemaphoreSlim startWait = new SemaphoreSlim (1, 1);

		readonly WorkbookAppInstallation workbookApp;

		readonly object runningAgentProcessStateLock = new object ();
		AgentProcessState runningAgentProcessState;

		public AgentProcessManager (WorkbookAppInstallation workbookApp)
		{
			TAG = $"{nameof (AgentProcessManager<TAgentProcess>)}<{typeof (TAgentProcess).Name}>";

			this.workbookApp = workbookApp;
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
				agentProcessState = new AgentProcessState (workbookApp);
				agentProcessState.AgentProcess.UnexpectedlyTerminated
					+= HandleAgentProcessUnexpectedlyTerminated;

				var identifyAgentRequest = ClientApp
					.SharedInstance
					.WebServer
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
					.WebServer
					.AgentIdentificationManager
					.GetAgentIdentityAsync (identifyAgentRequest).ConfigureAwait (false);

				var agentAssociable = agentProcessState.AgentProcess as IAgentAssociable;
				if (agentAssociable == null) {
					LogStartStatus (ticket, "creating agent client");
					agentProcessState.AgentClient = new AgentClient (
						agentProcessState.AgentIdentity.Host,
						agentProcessState.AgentIdentity.Port);
				} else {
					LogStartStatus (ticket, "waiting on agent association");
					var agentAssociation = await agentAssociable.GetAgentAssociationAsync (
						agentProcessState.AgentIdentity,
						cancellationToken).ConfigureAwait (false);
					agentProcessState.AgentIdentity = agentAssociation.Identity;
					agentProcessState.AgentClient = agentAssociation.Client;
				}

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
				throw e;
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
