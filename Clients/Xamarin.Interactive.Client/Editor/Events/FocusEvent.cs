//
// FocusEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

namespace Xamarin.Interactive.Editor.Events
{
	sealed class FocusEvent : EditorEvent
	{
		public FocusEvent (IEditor source) : base (source)
		{
		}
	}
}