//
// PreferencesTabViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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