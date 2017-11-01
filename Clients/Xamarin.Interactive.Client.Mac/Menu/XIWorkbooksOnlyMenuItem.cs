//
// XIWorkbooksOnlyMenuItem.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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
