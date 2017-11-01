//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Client.ViewControllers;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Client
{
    interface IClientSessionViewControllers
    {
        MessageViewController Messages { get; }
        History ReplHistory { get; }
        WorkbookTargetsViewController WorkbookTargets { get; }
    }
}