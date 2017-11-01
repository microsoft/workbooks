//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Client.Mac.Views
{
    [Register (nameof (XILabel))]
    class XILabel : NSTextField
    {
        public XILabel (IntPtr handle) : base (handle)
        {
            Initialize ();
        }

        public XILabel ()
        {
            Initialize ();
        }

        void Initialize ()
        {
            ControlSize = NSControlSize.Regular;
            Font = NSFont.SystemFontOfSize (NSFont.SystemFontSizeForControlSize (ControlSize));
            Bezeled = false;
            DrawsBackground = false;
            Editable = false;
            Selectable = false;
        }
    }
}