//
// UncachedAgentTicket.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client
{
	sealed class UncachedAgentTicket : IAgentTicket
	{
		readonly AgentClient client;
		readonly CancellationTokenSource monitorConnectionCancellationTokenSource
			= new CancellationTokenSource ();

		AgentIdentity agentIdentity;

		public ClientSessionUri ClientSessionUri { get; }
		public IReadOnlyList<string> AssemblySearchPaths { get; }
		public IMessageService MessageService { get; }
		public bool IsDisposed { get; private set; }

		public event EventHandler Disposed;

		public UncachedAgentTicket (
			ClientSessionUri clientSessionUri,
			IMessageService messageService,
			Action disconnectedHandler)
		{
			if (clientSessionUri == null)
				throw new ArgumentNullException (nameof (clientSessionUri));

			if (disconnectedHandler == null)
				throw new ArgumentNullException (nameof (disconnectedHandler));

			client = new AgentClient (
				clientSessionUri.Host,
				clientSessionUri.Port);

			ClientSessionUri = clientSessionUri;
			AssemblySearchPaths = clientSessionUri.AssemblySearchPaths;
			MessageService = messageService;

			client.OpenAgentMessageChannel (monitorConnectionCancellationTokenSource.Token)
			      .ContinueWithOnMainThread (task => disconnectedHandler ());
		}

		public void Dispose ()
		{
			if (!IsDisposed) {
				IsDisposed = true;
				Disposed?.Invoke (this, EventArgs.Empty);
				monitorConnectionCancellationTokenSource.Cancel ();
			}
		}

		public async Task<AgentIdentity> GetAgentIdentityAsync (CancellationToken cancellationToken)
			=>  agentIdentity ?? (agentIdentity = await client.GetAgentIdentityAsync (cancellationToken));

		public Task<AgentClient> GetClientAsync (CancellationToken cancellationToken)
			=> Task.FromResult (client);
	}
}