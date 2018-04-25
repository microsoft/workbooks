// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class AgentIdentityRequest : IXipRequestMessage<Agent>
    {
        public void Handle (Agent agent, Action<object> responseWriter)
            => responseWriter (agent.Identity);
    }
}