//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Xamarin.Interactive.Client.Web.Hosting;
using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Client.Web
{
    public sealed class InteractiveSessionHub : Hub
    {
        readonly IServiceProvider serviceProvider;

        public InteractiveSessionHub (IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public override Task OnConnectedAsync ()
        {
            serviceProvider
                .GetInteractiveSessionHubManager ()
                .OnConnectedAsync (Context.Connection);

            return base.OnConnectedAsync ();
        }

        public override Task OnDisconnectedAsync (Exception exception)
        {
            serviceProvider
                .GetInteractiveSessionHubManager ()
                .OnDisconnectedAsync (Context.Connection);

            return base.OnDisconnectedAsync (exception);
        }

        public Task OpenSession (string sessionUri)
        {
            if (!ClientSessionUri.TryParse (sessionUri, out var uri))
                throw new Exception ("Invalid client session URI");

            var connectionId = Context.ConnectionId;

            MainThread.Post (() => InitializeClientSessionAsync (connectionId, new ClientSession (uri)).Forget ());

            return Task.CompletedTask;
        }

        async Task InitializeClientSessionAsync (string connectionId, ClientSession session)
        {
            session.InitializeViewControllers (new WebClientSessionViewControllers (connectionId, serviceProvider));
            await session.InitializeAsync ();
            await session.EnsureAgentConnectionAsync ();

            serviceProvider
                .GetInteractiveSessionHubManager ()
                .BindClientSession (connectionId, session);
        }

        public async Task<string> InsertCodeCell (
            string initialBuffer,
            string relativeToCodeCellId,
            bool insertBefore)
        {
            var sessionState = serviceProvider
                .GetInteractiveSessionHubManager ()
                .GetSession (Context.ConnectionId);

            var codeCellState = await sessionState
                .EvaluationService
                .InsertCodeCellAsync (
                    initialBuffer,
                    relativeToCodeCellId,
                    insertBefore,
                    Context.Connection.ConnectionAbortedToken);

            return codeCellState.Id;
        }

        public Task Evaluate (string targetCodeCellId, bool evaluateAll)
        {
            var sessionState = serviceProvider
                .GetInteractiveSessionHubManager ()
                .GetSession (Context.ConnectionId);

            return sessionState.EvaluationService.EvaluateAsync (
                targetCodeCellId,
                evaluateAll,
                Context.Connection.ConnectionAbortedToken);
        }
    }
}