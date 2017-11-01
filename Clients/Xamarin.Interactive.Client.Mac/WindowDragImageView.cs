//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Client.Mac
{
    [Register (nameof (WindowDragImageView))]
    sealed class WindowDragImageView : NSImageView
    {
        WindowDragImageView (IntPtr handle) : base (handle)
        {
        }

        public override bool MouseDownCanMoveWindow => true;
    }
}