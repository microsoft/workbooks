//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Client
{
    sealed class AgentConnection : IAgentConnection, IDisposable
    {
        readonly IAgentTicket ticket;

        public AgentType Type { get; }
        public AgentIdentity Identity { get; }
        public AgentClient Api { get; }
        public AgentFeatures Features { get; }

        public bool IsConnected
            => ticket != null && !ticket.IsDisposed && Api != null;

        public AgentConnection (AgentType agentType)
        {
            Type = agentType;
        }

        AgentConnection (
            IAgentTicket ticket,
            AgentType type,
            AgentIdentity identity,
            AgentClient apiClient,
            AgentFeatures features)
        {
            this.ticket = ticket;

            Type = type;
            Identity = identity;
            Api = apiClient;
            Features = features;
        }

        void IDisposable.Dispose ()
        {
            ticket?.Dispose ();
        }

        public AgentConnection WithAgentType (AgentType agentType)
        {
            if (agentType == Type)
                return this;

            return new AgentConnection (
                ticket,
                agentType,
                Identity,
                Api,
                Features);
        }

        public async Task<AgentConnection> ConnectAsync (
            IWorkbookAppInstallation workbookApp,
            ClientSessionUri clientSessionUri,
            IMessageService messageService,
            Action disconnectedHandler,
            CancellationToken cancellationToken)
        {
            if (disconnectedHandler == null)
                throw new ArgumentNullException (nameof (disconnectedHandler));

            IAgentTicket ticket;

            if (clientSessionUri == null ||
                clientSessionUri.Host == null ||
                !ValidPortRange.IsValid (clientSessionUri.Port))
                ticket = await workbookApp.RequestAgentTicketAsync (
                    clientSessionUri,
                    messageService,
                    disconnectedHandler,
                    cancellationToken);
            else
                ticket = new UncachedAgentTicket (
                    clientSessionUri,
                    messageService,
                    disconnectedHandler);

            var identity = await ticket.GetAgentIdentityAsync (cancellationToken);
            if (identity == null)
                throw new Exception ("IAgentTicket.GetAgentIdentityAsync did not return an identity");

            var apiClient = await ticket.GetClientAsync (cancellationToken);
            if (apiClient == null)
                throw new Exception ("IAgentTicket.GetClientAsync did not return a client");

            apiClient.SessionCancellationToken = cancellationToken;

            return new AgentConnection (
                ticket,
                identity.AgentType,
                identity,
                apiClient,
                await apiClient.GetAgentFeaturesAsync (cancellationToken));
        }

        public async Task<AgentConnection> RefreshFeaturesAsync ()
        {
            if (!IsConnected)
                throw new InvalidOperationException ("Not connected to agent");

            var features = await Api.GetAgentFeaturesAsync ();

            if (features == Features || (Features != null && Features.Equals (features)))
                return this;

            return new AgentConnection (
                ticket,
                Type,
                Identity,
                Api,
                features);
        }

        public AgentConnection TerminateConnection ()
        {
            if (IsConnected)
                ((IDisposable)this).Dispose ();

            return new AgentConnection (Type);
        }
    }
}