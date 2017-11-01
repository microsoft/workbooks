//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class NewWorkbookWindow : NSWindow
    {
        NewWorkbookWindow (IntPtr handle) : base (handle)
        {
        }

        public override void AwakeFromNib ()
        {
            BackgroundColor = NSColor.White;

            MovableByWindowBackground = true;

            Title = Catalog.GetString ("New Workbook");

            Center ();
        }

        [Export ("close:")]
        public void Close (NSObject sender)
        {
            if (IsSheet && SheetParent != null)
                SheetParent.EndSheet (this);
            else
                PerformClose (sender);
        }
    }
}