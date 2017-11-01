//
// XIDebugMenuItem.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Client.Mac.Menu
{
	[Register ("XIDebugMenuItem")]
	sealed class XIDebugMenuItem : NSMenuItem
	{
		public XIDebugMenuItem (NSCoder coder) : base (coder)
		{
		}

		public XIDebugMenuItem (IntPtr handle) : base (handle)
		{
		}

		public override bool Hidden {
			get {
				return
#if DEBUG
					false;
#else
					true;
#endif
			}
			set { }
		}
	}
}
