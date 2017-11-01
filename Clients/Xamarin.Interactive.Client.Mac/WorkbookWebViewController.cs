//
// WorkbookWebViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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