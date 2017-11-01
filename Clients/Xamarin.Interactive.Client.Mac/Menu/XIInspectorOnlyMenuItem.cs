//
// XIInspectorOnlyMenuItem.cs
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
	[Register ("XIInspectorOnlyMenuItem")]
	sealed class XIInspectorOnlyMenuItem : NSMenuItem
	{
		public XIInspectorOnlyMenuItem (NSCoder coder) : base (coder)
		{
		}

		public XIInspectorOnlyMenuItem (IntPtr handle) : base (handle)
		{
		}

		public override bool Hidden {
			get { return ClientInfo.Flavor != ClientFlavor.Inspector; }
			set { }
		}
	}
}