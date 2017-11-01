//
// StatusToolbarView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.


using System;

using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	sealed partial class StatusToolbarView : NSButton
	{
		StatusToolbarView (IntPtr handle) : base (handle)
		{
			Cell = new NSButtonCell ();
			BezelStyle = NSBezelStyle.TexturedRounded;
			Title = String.Empty;
			Enabled = false;
		}

		public override bool MouseDownCanMoveWindow => true;
	}
}