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
    [Register ("AutoFitTextField")]
    sealed class AutoFitTextField : NSTextField
    {
        public AutoFitTextField (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public AutoFitTextField (NSCoder coder) : base (coder)
        {
        }

        public override void AwakeFromNib ()
        {
            var frame = Frame;
            while (true) {
                var size = AttributedStringValue.Size;
                if (size.Width <= frame.Width && size.Height <= frame.Height)
                    break;
                Font = NSFont.FromDescription (Font.FontDescriptor, Font.PointSize - 0.25f);
            }
        }
    }
}