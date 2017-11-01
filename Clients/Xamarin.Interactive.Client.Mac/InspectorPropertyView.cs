//
// InspectorPropertyView.cs
//
// Author:
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//
// Copyright 2014 Xamarin Inc.

using System;

using Foundation;
using AppKit;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Mac
{
	[Register ("InspectorPropertyView")]
	partial class InspectorPropertyView : NSTableView
	{
		public InspectorPropertyView (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		[Export ("initWithCoder:")]
		public InspectorPropertyView (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		void Initialize ()
		{
		}

		public void SelectView (InspectView target)
		{
		}
	}
}