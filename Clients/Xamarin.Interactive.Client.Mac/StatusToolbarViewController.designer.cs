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
	[Register ("StatusToolbarViewController")]
	partial class StatusToolbarViewController
	{
		[Outlet]
		AppKit.NSButton actionButton { get; set; }

		[Outlet]
		AppKit.NSImageView imageView { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator indeterminateProgressIndicator { get; set; }

		[Outlet]
		AppKit.NSStackView stackView { get; set; }

		[Outlet]
		AppKit.NSTextField textField { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (stackView != null) {
				stackView.Dispose ();
				stackView = null;
			}

			if (actionButton != null) {
				actionButton.Dispose ();
				actionButton = null;
			}

			if (imageView != null) {
				imageView.Dispose ();
				imageView = null;
			}

			if (indeterminateProgressIndicator != null) {
				indeterminateProgressIndicator.Dispose ();
				indeterminateProgressIndicator = null;
			}

			if (textField != null) {
				textField.Dispose ();
				textField = null;
			}
		}
	}
}
