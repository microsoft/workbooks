//
// UpdaterCheckingViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	partial class UpdaterCheckingViewController : NSViewController
	{
		UpdaterCheckingViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			progressIndicator.StartAnimation (this);

			base.ViewDidLoad ();
		}

		[Action ("cancelCheck:")]
		void CancelCheck (NSObject sender)
			=> View.Window.PerformClose (sender);
	}
}