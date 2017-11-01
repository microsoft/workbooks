//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;

namespace Xamarin.Interactive.Workbook.Events
{
    sealed class NavigateReplHistoryEvent : EditorEvent
    {
        public bool NavigatePrevious { get; }
        public bool Handled { get; set; }

        public NavigateReplHistoryEvent (IEditor source, LinePosition cursor, bool navigatePrevious)
            : base (source, cursor)
            => NavigatePrevious = navigatePrevious;
    }
}