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
    sealed class EvaluateCodeCellEvent : EditorEvent
    {
        public EvaluateCodeCellEvent (IEditor source, LinePosition cursor) : base (source, cursor)
        {
        }
    }
}