//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Editor.Events
{
    sealed class FocusEvent : EditorEvent
    {
        public FocusEvent (IEditor source) : base (source)
        {
        }
    }
}