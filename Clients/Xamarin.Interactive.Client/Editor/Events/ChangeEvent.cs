//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Events;

namespace Xamarin.Interactive.Editor.Events
{
    sealed class ChangeEvent : EditorEvent, IDocumentDirtyEvent
    {
        public string Text { get; }

        public ChangeEvent (IEditor source) : base (source)
        {
        }

        public ChangeEvent (IEditor source, string text)
            : base (source)
            => Text = text;
    }
}