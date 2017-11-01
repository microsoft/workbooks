//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Client.Mac.Menu
{
    [Register ("XIWorkbooksOnlyMenuItem")]
    sealed class XIWorkbooksOnlyMenuItem : NSMenuItem
    {
        public XIWorkbooksOnlyMenuItem (NSCoder coder) : base (coder)
        {
        }

        public XIWorkbooksOnlyMenuItem (IntPtr handle) : base (handle)
        {
        }

        public override bool Hidden {
            get { return ClientInfo.Flavor != ClientFlavor.Workbooks; }
            set { }
        }
    }
}
