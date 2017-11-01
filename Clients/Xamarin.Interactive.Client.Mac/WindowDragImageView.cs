//
// WindowDragImage.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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