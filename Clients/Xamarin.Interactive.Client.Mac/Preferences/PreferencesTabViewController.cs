//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;

namespace Xamarin.Interactive.Preferences
{
    sealed partial class PreferencesTabViewController : NSTabViewController
    {
        PreferencesTabViewController (IntPtr handle) : base (handle)
        {
        }

        public override void DidSelect (NSTabView tabView, NSTabViewItem item)
        {
            base.DidSelect (tabView, item);

            if (View?.Window?.WindowController is PreferencesWindowController windowController)
                windowController.DidSelectTabViewItem (item);
        }
    }
}