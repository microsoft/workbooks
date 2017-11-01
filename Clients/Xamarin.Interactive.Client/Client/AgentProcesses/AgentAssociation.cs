//
// AgentAssociation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    sealed class AgentAssociation
    {
        public AgentIdentity Identity { get; }
        public AgentClient Client { get; }

        public AgentAssociation (AgentIdentity identity, AgentClient client)
        {
            if (identity == null)
                throw new ArgumentNullException (nameof (identity));

            if (client == null)
                throw new ArgumentNullException (nameof (client));

            Identity = identity;
            Client = client;
        }
    }
}