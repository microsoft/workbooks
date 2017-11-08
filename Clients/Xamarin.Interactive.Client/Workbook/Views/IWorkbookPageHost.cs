//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Views
{
    interface IWorkbookPageHost
    {
        IEnumerable<ClientSessionTaskDelegate> GetClientSessionInitializationTasks (Uri clientWebServerUri);
        WorkbookPageViewModel CreatePageViewModel (ClientSession clientSession, WorkbookPage workbookPage);
    }
}