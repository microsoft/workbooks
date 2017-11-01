//
// NewWorkbookWindow.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Mac
{
	sealed partial class NewWorkbookWindow : NSWindow
	{
		NewWorkbookWindow (IntPtr handle) : base (handle)
		{
		}

		public override void AwakeFromNib ()
		{
			BackgroundColor = NSColor.White;

			MovableByWindowBackground = true;

			Title = Catalog.GetString ("New Workbook");

			Center ();
		}

		[Export ("close:")]
		public void Close (NSObject sender)
		{
			if (IsSheet && SheetParent != null)
				SheetParent.EndSheet (this);
			else
				PerformClose (sender);
		}
	}
}