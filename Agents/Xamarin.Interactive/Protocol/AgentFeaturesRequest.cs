// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class AgentFeaturesRequest : MainThreadRequest<AgentFeatures>
    {
        protected override Task<AgentFeatures> HandleAsync (Agent agent)
            => Task.FromResult(new AgentFeatures (agent
                .ViewHierarchyHandlerManager
                .AvailableHierarchyKinds
                .ToArray ()));
    }
}