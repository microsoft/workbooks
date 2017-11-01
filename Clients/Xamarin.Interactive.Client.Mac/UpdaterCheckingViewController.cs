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
    partial class UpdaterCheckingViewController : NSViewController
    {
        UpdaterCheckingViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            progressIndicator.StartAnimation (this);

            base.ViewDidLoad ();
        }

        [Action ("cancelCheck:")]
        void CancelCheck (NSObject sender)
            => View.Window.PerformClose (sender);
    }
}