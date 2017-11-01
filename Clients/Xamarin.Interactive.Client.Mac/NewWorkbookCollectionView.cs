//
// NewWorkbookCollectionView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	partial class NewWorkbookCollectionView : NSCollectionView
	{
		NewWorkbookCollectionView (IntPtr handle) : base (handle)
		{
		}

		public override void KeyDown (NSEvent theEvent)
		{
			switch (theEvent.KeyCode) {
			case 36:
			case 76:
				Superview.KeyDown (theEvent);
				break;
			default:
				base.KeyDown (theEvent);
				break;
			}
		}
	}
}