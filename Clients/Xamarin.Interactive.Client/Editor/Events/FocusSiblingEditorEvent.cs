//
// FocusSiblingEditorEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

namespace Xamarin.Interactive.Editor.Events
{
	sealed class FocusSiblingEditorEvent : EditorEvent
	{
		public enum WhichEditor
		{
			Previous,
			Next
		}

		public WhichEditor Which { get; }

		public FocusSiblingEditorEvent (IEditor source, WhichEditor which) : base (source)
		{
			Which = which;
		}
	}
}