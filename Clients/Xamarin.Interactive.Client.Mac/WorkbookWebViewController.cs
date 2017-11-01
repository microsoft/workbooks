//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class WorkbookWebViewController : SessionViewController
    {
        WorkbookWebViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidAppear ()
        {
            base.ViewDidAppear ();

            webView.SubscribeToPreferences ();
        }

        public override void ViewDidDisappear ()
        {
            base.ViewDidDisappear ();

            webView.UnsubscribeFromPreferences ();
        }

        protected override void OnSessionAvailable ()
            => Session.InitializeAsync (webView.XcbWebView).Forget ();
    }
}