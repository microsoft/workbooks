//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class StatusToolbarView : NSButton
    {
        StatusToolbarView (IntPtr handle) : base (handle)
        {
            Cell = new NSButtonCell ();
            BezelStyle = NSBezelStyle.TexturedRounded;
            Title = String.Empty;
            Enabled = false;
        }

        public override bool MouseDownCanMoveWindow => true;
    }
}