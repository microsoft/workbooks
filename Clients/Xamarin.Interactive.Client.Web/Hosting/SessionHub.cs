//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

namespace Xamarin.Interactive.Client.Web.Hosting
{
    public sealed class SessionHub : Hub
    {
        public Task Send (string message)
            => Clients.All.SendAsync ("Send", message);
    }
}