//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Events;

namespace Xamarin.Interactive.Editor.Events
{
    abstract class EditorEvent : IEvent
    {
        object IEvent.Source => Source;

        public IEditor Source { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        protected EditorEvent (IEditor source)
            => Source = source ?? throw new ArgumentNullException (nameof (source));
    }
}