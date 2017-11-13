//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.Events;

namespace Xamarin.Interactive.Editor.Events
{
    sealed class ChangeEvent : EditorEvent, IDocumentDirtyEvent
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