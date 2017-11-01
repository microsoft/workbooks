//
// XILabel.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Client.Mac.Views
{
	[Register (nameof (XILabel))]
	class XILabel : NSTextField
	{
		public XILabel (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		public XILabel ()
		{
			Initialize ();
		}

		void Initialize ()
		{
			ControlSize = NSControlSize.Regular;
			Font = NSFont.SystemFontOfSize (NSFont.SystemFontSizeForControlSize (ControlSize));
			Bezeled = false;
			DrawsBackground = false;
			Editable = false;
			Selectable = false;
		}
	}
}