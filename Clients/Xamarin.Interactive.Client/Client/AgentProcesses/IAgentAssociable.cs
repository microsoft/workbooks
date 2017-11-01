//
// IAgentClientFactory.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.AgentProcesses
{
	interface IAgentAssociable
	{
		Task<AgentAssociation> GetAgentAssociationAsync (
			AgentIdentity agentIdentity,
			CancellationToken cancellationToken);
	}
}