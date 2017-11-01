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
	[Register ("PreferencesGeneralViewController")]
	partial class PreferencesGeneralViewController
	{
		[Outlet]
		AppKit.NSStepper fontSizeStepper { get; set; }

		[Outlet]
		AppKit.NSTextField fontSizeTextField { get; set; }

		[Outlet]
		AppKit.NSButton saveHistoryCheckButton { get; set; }

		[Outlet]
		AppKit.NSButton showExecutionTimingsCheckButton { get; set; }

		[Outlet]
		AppKit.NSButton showLineNumbersCheckButton { get; set; }

		[Action ("ResetAllPreferences:")]
		partial void ResetAllPreferences (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (fontSizeStepper != null) {
				fontSizeStepper.Dispose ();
				fontSizeStepper = null;
			}

			if (fontSizeTextField != null) {
				fontSizeTextField.Dispose ();
				fontSizeTextField = null;
			}

			if (saveHistoryCheckButton != null) {
				saveHistoryCheckButton.Dispose ();
				saveHistoryCheckButton = null;
			}

			if (showExecutionTimingsCheckButton != null) {
				showExecutionTimingsCheckButton.Dispose ();
				showExecutionTimingsCheckButton = null;
			}

			if (showLineNumbersCheckButton != null) {
				showLineNumbersCheckButton.Dispose ();
				showLineNumbersCheckButton = null;
			}
		}
	}
}
