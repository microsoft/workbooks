//
// IAgentTicket.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client
{
	interface IAgentTicket : INotifyDisposed
	{
		ClientSessionUri ClientSessionUri { get; }
		IReadOnlyList<string> AssemblySearchPaths { get; }

		IMessageService MessageService { get; }

		Task<AgentIdentity> GetAgentIdentityAsync (
			CancellationToken cancellationToken = default (CancellationToken));

		Task<AgentClient> GetClientAsync (
			CancellationToken cancellationToken = default (CancellationToken));
	}
}