//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Workbook.Models;
using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Client.Web.Hosting
{
    sealed class WebWorkbookPageHost : IWorkbookPageHost
    {
        readonly IServiceProvider serviceProvider;

        public WebWorkbookPageHost (IServiceProvider serviceProvider)
            => this.serviceProvider = serviceProvider;

        public WorkbookPageViewModel CreatePageViewModel (
            ClientSession clientSession,
            WorkbookPage workbookPage)
            => new WebWorkbookPageViewModel (serviceProvider, clientSession, workbookPage);

        public IEnumerable<ClientSessionTaskDelegate> GetClientSessionInitializationTasks (Uri clientWebServerUri)
            => Array.Empty<ClientSessionTaskDelegate> ();
    }
}