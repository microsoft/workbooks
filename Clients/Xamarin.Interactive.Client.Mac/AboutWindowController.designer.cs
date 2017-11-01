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
	[Register ("AboutWindowController")]
	partial class AboutWindowController
	{
		[Outlet]
		AppKit.NSTextField appNameLabel { get; set; }

		[Outlet]
		AppKit.NSTextField copyrightLabel { get; set; }

		[Outlet]
		AppKit.NSTextField versionLabel { get; set; }

		[Action ("ShowForums:")]
		partial void ShowForums (Foundation.NSObject sender);

		[Action ("ShowHelp:")]
		partial void ShowHelp (Foundation.NSObject sender);

		[Action ("ShowLicenses:")]
		partial void ShowLicenses (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (versionLabel != null) {
				versionLabel.Dispose ();
				versionLabel = null;
			}

			if (appNameLabel != null) {
				appNameLabel.Dispose ();
				appNameLabel = null;
			}

			if (copyrightLabel != null) {
				copyrightLabel.Dispose ();
				copyrightLabel = null;
			}
		}
	}
}
