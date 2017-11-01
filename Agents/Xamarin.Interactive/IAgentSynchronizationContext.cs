//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace Xamarin.Interactive
{
    public interface IAgentSynchronizationContext
    {
        SynchronizationContext PushContext (Action<Action> postHandler, Action<Action> sendHandler = null);
        SynchronizationContext PushContext (SynchronizationContext context);
        SynchronizationContext PeekContext ();
        SynchronizationContext PopContext ();
    }
}