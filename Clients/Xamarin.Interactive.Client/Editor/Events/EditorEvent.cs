//
// IEditorEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.Events;

namespace Xamarin.Interactive.Editor.Events
{
	abstract class EditorEvent : IEvent
	{
		object IEvent.Source => Source;

		public IEditor Source { get; }
		public DateTime Timestamp { get; } = DateTime.UtcNow;
		public LinePosition Cursor { get; }

		protected EditorEvent (IEditor source, LinePosition cursor = default(LinePosition))
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));

			Source = source;
			Cursor = cursor;
		}
	}
}