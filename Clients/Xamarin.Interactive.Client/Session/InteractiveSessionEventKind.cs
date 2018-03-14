// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Session
{
    public enum InteractiveSessionEventKind
    {
        None,
        ConnectingToAgent,
        InitializingWorkspace,
        Ready,
        AgentFeaturesUpdated,
        AgentDisconnected,
        Evaluation
    }
}