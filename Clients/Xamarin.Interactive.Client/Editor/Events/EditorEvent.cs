//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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