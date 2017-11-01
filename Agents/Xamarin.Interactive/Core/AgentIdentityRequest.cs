//
// AgentIdentityRequest.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;

using Xamarin.Interactive.Protocol;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class AgentIdentityRequest : IXipRequestMessage<Agent>
	{
		public Guid MessageId { get; } = Guid.NewGuid ();

		public void Handle (Agent agent, Action<object> responseWriter)
			=> responseWriter (agent.Identity);
	}
}