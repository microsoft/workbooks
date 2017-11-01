//
// SessionTabView.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	[Register ("SessionTabView")]
	class SessionTabView : NSTabView
	{
		SessionTabView (IntPtr handle) : base (handle)
		{
		}

		// NSTabViewController always tries to make its underlying NSTabView layer-backed (in order to support
		// transitions, presumably). We need to prevent this because if ReplWebView is layer-backed it results
		// in Monaco rendering glitches and an invisible map renderer.
		public override bool WantsLayer {
			get { return false; }
			set { }
		}
	}
}