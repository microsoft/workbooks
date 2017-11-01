//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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