//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    interface IAgentProcessState
    {
        IAgentProcess AgentProcess { get; }
        AgentIdentity AgentIdentity { get; }
        AgentClient AgentClient { get; }
    }
}
