//
// ScrollIntoViewOptions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using Foundation;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	[Protocol]
	interface IScrollIntoViewOptions : IJSExport
	{
		[Export ("behavior")]
		string Behavior { get; }

		[Export ("block")]
		string Block { get; }
	}

	public sealed class ScrollIntoViewOptions : NSObject, IScrollIntoViewOptions
	{
		public ScrollIntoViewBehavior Behavior { get; set; }

		string IScrollIntoViewOptions.Behavior {
			get { return Behavior.ToString ().ToLowerInvariant (); }
		}

		public ScrollIntoViewBlock Block { get; set; }

		string IScrollIntoViewOptions.Block {
			get { return Block.ToString ().ToLowerInvariant (); }
		}
	}
}