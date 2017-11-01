//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class PackageManagerWindow : NSWindow
    {
        public PackageManagerWindow (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public PackageManagerWindow (NSCoder coder) : base (coder)
        {
        }

        public override void AwakeFromNib ()
        {
            base.AwakeFromNib ();
        }
    }
}
