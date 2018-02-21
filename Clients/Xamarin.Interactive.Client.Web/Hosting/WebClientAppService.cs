//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Client.Web.Hosting
{
    public sealed class WebClientAppService
    {
        internal ClientApp ClientApp { get; }

        public WebClientAppService ()
        {
            ClientApp = new WebClientApp ();
            ClientApp.Initialize (asSharedInstance: true);
        }

        internal async Task<ClientSession> OpenClientSessionAsync (
            string sessionUri,
            CancellationToken cancellationToken = default)
        {
            if (!ClientSessionUri.TryParse (sessionUri, out var uri))
                throw new Exception ("Invalid client session URI");

            var session = new ClientSession (uri);
            session.InitializeViewControllers (new WebClientSessionViewControllers ());
            await session.InitializeAsync (new WebWorkbookPageHost ());

            return session;
        }
    }
}