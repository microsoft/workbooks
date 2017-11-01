//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Tests
{
    sealed class TestAgent : Agent
    {
        static Stack<Action<TestAgent>> allocHandlers = new Stack<Action<TestAgent>> ();

        public static void PushAllocHandler (Action<TestAgent> handler)
        {
            lock (allocHandlers)
                allocHandlers.Push (handler);
        }

        public event EventHandler Stopped;

        public IdentifyAgentRequest IdentifyAgentRequest { get; set; }

        public TestAgent (IdentifyAgentRequest identifyAgentRequest)
            : base (unitTestContext: true)
        {
            Identity = new AgentIdentity (
                AgentType.Test,
                Sdk.FromEntryAssembly ("Test"),
                nameof (TestAgent));

            IdentifyAgentRequest = identifyAgentRequest;

            Action<TestAgent> allocHandler = null;

            lock (allocHandlers) {
                if (allocHandlers.Count > 0)
                    allocHandler = allocHandlers.Pop ();
            }

            allocHandler?.Invoke (this);
        }

        public new void Stop ()
        {
            base.Stop ();
            Stopped?.Invoke (this, EventArgs.Empty);
        }

        protected override IdentifyAgentRequest GetIdentifyAgentRequest ()
            => IdentifyAgentRequest;

        public override InspectView GetVisualTree (string hierarchyKind)
        {
            throw new NotImplementedException ();
        }
    }
}