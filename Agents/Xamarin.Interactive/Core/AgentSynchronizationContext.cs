//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Core
{
    sealed class AgentSynchronizationContext : StackSynchronizationContext, IAgentSynchronizationContext
    {
        public AgentSynchronizationContext () : base (withCurrentSynchronizationContext: true)
        {
            SetSynchronizationContext (this);
            MainThread.Initialize ();
        }

        public SynchronizationContext PushContext (
            Action<Action> postHandler,
            Action<Action> sendHandler = null)
            => PushContext (new ActionSynchronizationContext (postHandler, sendHandler));

        protected override bool CanPopContext (SynchronizationContext currentContext, int totalContexts)
            => totalContexts > 1;

        protected override void OnContextChanged ()
            => new TaskFactory (TaskScheduler.FromCurrentSynchronizationContext ())
                .StartNew (MainThread.Reinitialize);
    }
}