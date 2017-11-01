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
	[Register ("PreferencesUpdaterViewController")]
	partial class PreferencesUpdaterViewController
	{
		[Outlet]
		AppKit.NSTextField channelTextField { get; set; }

		[Outlet]
		AppKit.NSPopUpButton frequencyPopUpButton { get; set; }

		[Outlet]
		AppKit.NSTextField lastCheckedTextField { get; set; }

		[Outlet]
		AppKit.NSButton switchChannelsButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (frequencyPopUpButton != null) {
				frequencyPopUpButton.Dispose ();
				frequencyPopUpButton = null;
			}

			if (channelTextField != null) {
				channelTextField.Dispose ();
				channelTextField = null;
			}

			if (lastCheckedTextField != null) {
				lastCheckedTextField.Dispose ();
				lastCheckedTextField = null;
			}

			if (switchChannelsButton != null) {
				switchChannelsButton.Dispose ();
				switchChannelsButton = null;
			}
		}
	}
}
