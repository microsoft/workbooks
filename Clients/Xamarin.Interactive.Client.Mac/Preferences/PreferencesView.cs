//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using CoreGraphics;

namespace Xamarin.Interactive.Preferences
{
    sealed partial class PreferencesView : NSView
    {
        bool intrinsicContentSizeSet;
        CGSize intrinsicContentSize;
        public override CGSize IntrinsicContentSize => intrinsicContentSize;

        PreferencesView (IntPtr handle) : base (handle)
        {
            intrinsicContentSize = new CGSize (NoIntrinsicMetric, NoIntrinsicMetric);
        }

        public override void AwakeFromNib ()
        {
            base.AwakeFromNib ();

            if (!intrinsicContentSizeSet)
                UpdateIntrinsicContentSize (Frame.Size);
        }

        public void UpdateIntrinsicContentSize (CGSize size)
        {
            intrinsicContentSizeSet = true;
            intrinsicContentSize = size;
            InvalidateIntrinsicContentSize ();

            (Window?.WindowController as PreferencesWindowController)
                ?.NotifyIntrinsicContentSizeUpdated (this);
        }
    }
}