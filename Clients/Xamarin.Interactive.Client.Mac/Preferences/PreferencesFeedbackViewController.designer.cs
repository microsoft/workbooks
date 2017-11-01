// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Xamarin.Interactive.Preferences
{
	[Register ("PreferencesFeedbackViewController")]
	partial class PreferencesFeedbackViewController
	{
		[Outlet]
		AppKit.NSLayoutConstraint noticeHeightConstraint { get; set; }

		[Outlet]
		AppKit.NSTextView noticeTextView { get; set; }

		[Outlet]
		AppKit.NSButton optInRadioButton { get; set; }

		[Outlet]
		AppKit.NSButton optOutRadioButton { get; set; }

		[Action ("OptInOutActivated:")]
		partial void OptInOutActivated (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (noticeHeightConstraint != null) {
				noticeHeightConstraint.Dispose ();
				noticeHeightConstraint = null;
			}

			if (noticeTextView != null) {
				noticeTextView.Dispose ();
				noticeTextView = null;
			}

			if (optInRadioButton != null) {
				optInRadioButton.Dispose ();
				optInRadioButton = null;
			}

			if (optOutRadioButton != null) {
				optOutRadioButton.Dispose ();
				optOutRadioButton = null;
			}
		}
	}
}
