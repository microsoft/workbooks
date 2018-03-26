//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;

namespace Xamarin.Interactive.Workbook.Events
{
    sealed class NavigateReplHistoryEvent : EditorEvent
    {
        public Position Cursor { get; }
        public bool NavigatePrevious { get; }
        public bool Handled { get; set; }

        public NavigateReplHistoryEvent (IEditor source, Position cursor, bool navigatePrevious)
            : base (source)
        {
            Cursor = cursor;
            NavigatePrevious = navigatePrevious;
        }
    }
}