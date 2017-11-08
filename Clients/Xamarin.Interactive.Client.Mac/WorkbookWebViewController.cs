//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class WorkbookWebViewController : SessionViewController
    {
        IWorkbookPageHost workbookPageViewHost;

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
        {
            if (workbookPageViewHost == null)
                workbookPageViewHost = new WorkbookWebPageViewHost (webView.XcbWebView);

            Session.InitializeAsync (workbookPageViewHost).Forget ();
        }
    }
}