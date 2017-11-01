//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class LogWindow : NSPanel
    {
        public LogWindow (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public LogWindow (NSCoder coder) : base (coder)
        {
        }

        public override void AwakeFromNib ()
        {
            Level = NSWindowLevel.Normal;
            TitleVisibility = NSWindowTitleVisibility.Hidden;
        }
    }
}