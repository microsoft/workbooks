//
// SessionWindow.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class SessionWindow : NSWindow
    {
        SessionWindow (IntPtr handle) : base (handle)
        {
        }
    }
}