//
// ChangeEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using Microsoft.CodeAnalysis.Text;

namespace Xamarin.Interactive.Editor.Events
{
	sealed class ChangeEvent : EditorEvent
	{
		public string Text { get; }

		public ChangeEvent (IEditor source) : base (source, default (LinePosition))
		{
		}

		public ChangeEvent (IEditor source, LinePosition cursor, string text)
			: base (source, cursor)
		{
			Text = text;
		}

		public override string ToString ()
		{
			return $"@ {Cursor}: |{Text}|";
		}
	}
}