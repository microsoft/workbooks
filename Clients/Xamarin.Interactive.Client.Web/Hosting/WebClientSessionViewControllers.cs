//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Client.ViewControllers;
using Xamarin.Interactive.Client.Web.Models;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Client.Web.Hosting
{
    sealed class WebClientSessionViewControllers : IClientSessionViewControllers
    {
        public MessageViewController Messages { get; }

        public History ReplHistory { get; }
            = new History (
                Array.Empty<string> (),
                persist: false);

        public WorkbookTargetsViewController WorkbookTargets { get; }
            = new WorkbookTargetsViewController ();

        public WebClientSessionViewControllers (ClientConnectionId connectionId, IServiceProvider serviceProvider)
        {
            var hubManager = serviceProvider.GetInteractiveSessionHubManager ();
            Messages = new MessageViewController (
                new StatusMessageViewDelegate (connectionId, hubManager),
                new AlertMessageViewDelegate (connectionId, hubManager));
        }

        sealed class StatusMessageViewDelegate : IStatusMessageViewDelegate
        {
            readonly ClientConnectionId connectionId;
            readonly InteractiveSessionHubManager hubManager;

            public StatusMessageViewDelegate (ClientConnectionId connectionId, InteractiveSessionHubManager hubManager)
            {
                this.connectionId = connectionId;
                this.hubManager = hubManager;
            }

            public bool CanDisplayMessage (Message message) => true;

            public void DisplayIdle ()
                => hubManager.SendStatusUIAction (connectionId, StatusUIAction.DisplayIdle);

            public void DisplayMessage (Message message)
                => hubManager.SendStatusUIAction (connectionId, StatusUIAction.DisplayMessage, message);

            public void StartSpinner ()
                => hubManager.SendStatusUIAction (connectionId, StatusUIAction.StartSpinner);

            public void StopSpinner ()
                => hubManager.SendStatusUIAction (connectionId, StatusUIAction.StopSpinner);
        }

        sealed class AlertMessageViewDelegate : IAlertMessageViewDelegate
        {
            readonly ClientConnectionId connectionId;
            readonly InteractiveSessionHubManager hubManager;

            public AlertMessageViewDelegate (ClientConnectionId connectionId, InteractiveSessionHubManager hubManager)
            {
                this.connectionId = connectionId;
                this.hubManager = hubManager;
            }

            public void DismissMessage (int messageId)
            {
            }

            public void DisplayMessage (Message message)
            {
            }
        }
    }
}