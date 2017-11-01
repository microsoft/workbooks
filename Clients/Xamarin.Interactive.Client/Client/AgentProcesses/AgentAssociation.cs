//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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