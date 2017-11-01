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
    sealed partial class SessionWindow : NSWindow
    {
        SessionWindow (IntPtr handle) : base (handle)
        {
        }
    }
}