//
// CenteringToolbarItem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;

using Foundation;
using CoreGraphics;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	sealed class CenteringToolbarItem : NSToolbarItem
	{
		public CenteringToolbarItem (string itemIdentifier) : base (itemIdentifier)
		{
			NSNotificationCenter.DefaultCenter.AddObserver (
				NSWindow.DidResizeNotification,
				notification => UpdateWidth ()
			);
		}

		NSToolbarItem FindNextItem ()
		{
			var items = Toolbar.Items;
			for (int i = 0, n = items.Length; i < n; i++) {
				if (items [i] == this && i + 1 < n)
					return items [i + 1];
			}
			return null;
		}

		public override CGSize MinSize {
			get {
				// Create a view for the item so we'll get added
				// to the Superview we'll need on the next pass;
				// we don't care about its size... just can't be 0
				if (View == null)
					View = new NSView (new CGRect (0, 0, 1, 1));

				var nextItem = FindNextItem ();
				var window = View.Window;

				nfloat width = 0;

				if (window != null && nextItem?.View != null) {
					// the centering math here mostly makes sense except for an apparent
					// 2px of visual (render-only) padding that at least NSSegmentedControl
					// has when inside the item view (e.g. unaccounted for by the frame)
					var previousWidth = View.ConvertRectToView (View.Frame, null).X + 2;
					var nextWidth = nextItem.View.Frame.Width;
					var windowWidth = window.Frame.Width;

					width = NMath.Round (windowWidth / 2f - previousWidth - nextWidth / 2f);
				}

				return new CGSize (NMath.Max (0, width), base.MinSize.Height);
			}

			set { base.MinSize = value; }
		}

		public override CGSize MaxSize {
			get { return new CGSize (MinSize.Width, base.MaxSize.Height); }
			set { base.MaxSize = value; }
		}

		public void UpdateWidth ()
		{
			MinSize = MinSize;
			MaxSize = MaxSize;
		}
	}
}