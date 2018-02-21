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
        public WorkbookPageViewModel CreatePageViewModel (
            ClientSession clientSession,
            WorkbookPage workbookPage)
            => new WebWorkbookPageViewModel (clientSession, workbookPage);

        public IEnumerable<ClientSessionTaskDelegate> GetClientSessionInitializationTasks (Uri clientWebServerUri)
            => Array.Empty<ClientSessionTaskDelegate> ();
    }
}