//
// AboutWindowController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Foundation;
using AppKit;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Mac
{
	sealed partial class AboutWindowController : NSWindowController
	{
		NSMenu licensesMenu;

		public AboutWindowController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public AboutWindowController (NSCoder coder) : base (coder)
		{
		}

		public AboutWindowController () : base ("AboutWindow")
		{
		}

		public override void AwakeFromNib ()
		{
			versionLabel.StringValue = ClientInfo.AboutBoxVersionString;
			versionLabel.ToolTip = ClientInfo.AboutBoxVersionTooltip;
			appNameLabel.StringValue = ClientInfo.FullProductName;
			copyrightLabel.StringValue = BuildInfo.Copyright;

			licensesMenu = new NSMenu (Catalog.GetString ("Licenses & Notices"));

			licensesMenu.AddItem (new NSMenuItem (
				Catalog.GetString ("Microsoft Software License Terms…"),
				(o, e) => NSWorkspace.SharedWorkspace.OpenFile (
					NSBundle.MainBundle.PathForResource ("License", "rtf"))));

			licensesMenu.AddItem (new NSMenuItem (
				Catalog.GetString ("Third Party Notices…"),
				(o, e) => NSWorkspace.SharedWorkspace.OpenFile (
					NSBundle.MainBundle.PathForResource ("ThirdPartyNotices", "txt"))));

			licensesMenu.AddItem (new NSMenuItem (
				Catalog.GetString ("Microsoft Enterprise and Developer Privacy Statement…"),
				(o, e) => AppDelegate.SharedAppDelegate.ShowPrivacyStatement ((NSObject)o)));
		}

		partial void ShowForums (NSObject sender)
			=> AppDelegate.SharedAppDelegate.ShowForums (sender);

		partial void ShowHelp (NSObject sender)
			=> AppDelegate.SharedAppDelegate.ShowHelp (sender);

		partial void ShowLicenses (NSObject sender)
		{
			var button = (NSButton)sender;
			licensesMenu.PopUpMenu (
				null,
				new CoreGraphics.CGPoint (0, button.Frame.Height),
				button);
		}

		public new AboutWindow Window {
			get { return (AboutWindow)base.Window; }
		}
	}
}