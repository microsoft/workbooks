//
// LogWindow.cs
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
	sealed partial class LogWindow : NSPanel
	{
		public LogWindow (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public LogWindow (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			Level = NSWindowLevel.Normal;
			TitleVisibility = NSWindowTitleVisibility.Hidden;
		}
	}
}