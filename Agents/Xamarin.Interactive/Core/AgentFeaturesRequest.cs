//
// AgentFeaturesRequest.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class AgentFeaturesRequest : MainThreadRequest<AgentFeatures>
    {
        protected override Task<AgentFeatures> HandleAsync (Agent agent)
            => Task.FromResult(new AgentFeatures (agent
                .ViewHierarchyHandlerManager
                .AvailableHierarchyKinds
                .ToArray ()));
    }
}