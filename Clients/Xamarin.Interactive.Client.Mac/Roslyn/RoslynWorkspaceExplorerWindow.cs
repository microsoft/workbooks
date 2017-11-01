//
// RoslynWorkspaceExplorerWindow.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac.Roslyn
{
    sealed partial class RoslynWorkspaceExplorerWindow : NSWindow
    {
        public RoslynWorkspaceExplorerWindow (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public RoslynWorkspaceExplorerWindow (NSCoder coder) : base (coder)
        {
        }
    }
}