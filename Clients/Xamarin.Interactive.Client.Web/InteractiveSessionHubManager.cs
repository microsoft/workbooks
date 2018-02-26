//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Xamarin.Interactive.Client.Web.Models;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client.Web
{
    public sealed class InteractiveSessionHubManager : DefaultHubLifetimeManager<InteractiveSessionHub>
    {
        internal sealed class SessionState : IDisposable
        {
            public ClientSession ClientSession { get; set; }
            public EvaluationService EvaluationService { get; set; }
            public Observer<ICodeCellEvent> EvaluationEventObserver { get; set; }

            public void Dispose ()
            {
                ClientSession?.Dispose ();
                ClientSession = null;
            }
        }

        readonly ConcurrentDictionary<ClientConnectionId, SessionState> sessions
            = new ConcurrentDictionary<ClientConnectionId, SessionState> ();

        public override Task OnConnectedAsync (HubConnectionContext connection)
        {
            sessions.TryAdd (connection.ConnectionId, new SessionState ());

            return base.OnConnectedAsync (connection);
        }

        public override Task OnDisconnectedAsync (HubConnectionContext connection)
        {
            if (sessions.TryRemove (connection.ConnectionId, out var sessionState))
                sessionState.Dispose ();

            return base.OnDisconnectedAsync (connection);
        }

        internal void BindClientSession (ClientConnectionId connectionId, ClientSession clientSession)
        {
            if (sessions.TryGetValue (connectionId, out var sessionState)) {
                sessionState.ClientSession = clientSession;

                sessionState.EvaluationEventObserver = new Observer<ICodeCellEvent> (evnt => {
                    SendConnectionAsync (connectionId, "EvaluationEvent", new [] { evnt }).Forget ();
                });

                if (sessionState.ClientSession.EvaluationService is EvaluationService evaluationService) {
                    sessionState.EvaluationService = evaluationService;
                    evaluationService.Events.Subscribe (sessionState.EvaluationEventObserver);
                }
            }
        }

        internal void SendStatusUIAction (
            ClientConnectionId connectionId,
            StatusUIAction action,
            Message message = null)
            => SendConnectionAsync (
                connectionId,
                "statusUIAction",
                new object [] { action, message }).Forget ();

        internal SessionState GetSession (ClientConnectionId connectionId)
            => sessions.TryGetValue (connectionId, out var session) ? session : null;
    }
}