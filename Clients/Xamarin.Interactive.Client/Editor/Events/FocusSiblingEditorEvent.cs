//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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