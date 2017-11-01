//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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