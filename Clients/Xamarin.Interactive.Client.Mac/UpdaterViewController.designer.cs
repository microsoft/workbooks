// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Xamarin.Interactive.Client.Mac
{
	[Register ("UpdaterViewController")]
	partial class UpdaterViewController
	{
		[Outlet]
		AppKit.NSButton cancelButton { get; set; }

		[Outlet]
		AppKit.NSButton downloadButton { get; set; }

		[Outlet]
		AppKit.NSTextField messageLabel { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator progressBar { get; set; }

		[Outlet]
		AppKit.NSTextField progressLabel { get; set; }

		[Outlet]
		AppKit.NSButton remindMeLaterButton { get; set; }

		[Outlet]
		AppKit.NSTextField titleLabel { get; set; }

		[Outlet]
		WebKit.WebView webView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (downloadButton != null) {
				downloadButton.Dispose ();
				downloadButton = null;
			}

			if (messageLabel != null) {
				messageLabel.Dispose ();
				messageLabel = null;
			}

			if (remindMeLaterButton != null) {
				remindMeLaterButton.Dispose ();
				remindMeLaterButton = null;
			}

			if (titleLabel != null) {
				titleLabel.Dispose ();
				titleLabel = null;
			}

			if (webView != null) {
				webView.Dispose ();
				webView = null;
			}

			if (progressBar != null) {
				progressBar.Dispose ();
				progressBar = null;
			}

			if (progressLabel != null) {
				progressLabel.Dispose ();
				progressLabel = null;
			}

			if (cancelButton != null) {
				cancelButton.Dispose ();
				cancelButton = null;
			}
		}
	}
}
