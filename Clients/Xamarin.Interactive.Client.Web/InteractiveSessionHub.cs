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

namespace Xamarin.Interactive.Client.Web
{
    public sealed class InteractiveSessionHub : Hub
    {
        readonly IServiceProvider serviceProvider;

        public InteractiveSessionHub (IServiceProvider serviceProvider)
            => this.serviceProvider = serviceProvider;

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

            MainThread.Post (() => {
                var session = new ClientSession (uri);
                session.InitializeViewControllers (new WebClientSessionViewControllers (connectionId, serviceProvider));
                session.InitializeAsync (new WebWorkbookPageHost (serviceProvider)).ContinueWith (o => {
                    serviceProvider
                        .GetInteractiveSessionHubManager ()
                        .BindClientSession (connectionId, session);
                });
            });

            return Task.CompletedTask;
        }
    }
}