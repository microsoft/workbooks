//
// AboutWindow.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	sealed partial class AboutWindow : NSWindow
	{
		public AboutWindow (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public AboutWindow (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			MovableByWindowBackground = true;
			TitlebarAppearsTransparent = true;
			BackgroundColor = NSColor.White;

			StandardWindowButton (NSWindowButton.MiniaturizeButton).Hidden = true;
			StandardWindowButton (NSWindowButton.ZoomButton).Hidden = true;
		}
	}
}