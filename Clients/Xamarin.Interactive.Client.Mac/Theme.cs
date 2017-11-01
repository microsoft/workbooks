//
// Theme.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;

using AppKit;
using CoreGraphics;
using Foundation;

namespace Xamarin.Interactive.Client
{
	static class ThemeExtensions
	{
		public static class StatusBar
		{
			public static readonly NSColor IdleTextColor = NSColor.DisabledControlText;

			public static readonly NSColor InfoTextColor = NSColor.ControlText;

			public static readonly NSColor ErrorTextColor = NSColor.FromDeviceRgb (
				0xfa / (nfloat)0xff, 0x54 / (nfloat)0xff, 0x33 / (nfloat)0xff);
		}

		static NSImageRep GetIconRep (this Theme theme, string name, int size, bool selected, string suffix)
		{
			var composedName = theme.GetIconName (name, size, selected);
			var url = new NSUrl (
				$"Icons/{composedName}{suffix}.png",
				NSBundle.MainBundle.ResourceUrl);

			var rep = NSImageRep.ImageRepFromUrl (url);
			if (rep == null && suffix == "@2x")
				rep = theme.GetIconRep (name, size * 2, selected, null);

			return rep;
		}

		public static NSImage GetIcon (this Theme theme, string name, int size, bool selected = false)
		{
			var cacheKey = theme.GetIconName (name, size, selected);

			if (theme.TryGetCachedItem<NSImage> (cacheKey, out var icon))
				return icon;

			foreach (var suffix in new [] { "", "@2x" }) {
				var rep = theme.GetIconRep (name, size, selected, suffix);
				if (rep != null) {
					if (icon == null)
						icon = new NSImage (new CGSize (size, size));
					icon.AddRepresentation (rep);
				}
			}

			theme.CacheItem (cacheKey, icon);

			return icon;
		}
	}
}