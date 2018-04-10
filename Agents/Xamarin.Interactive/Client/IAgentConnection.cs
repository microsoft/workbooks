// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client
{
    interface IAgentConnection
    {
        AgentType Type { get; }
        AgentIdentity Identity { get; }
        AgentClient Api { get; }
        AgentFeatures Features { get; }
        bool IsConnected { get; }
    }
}