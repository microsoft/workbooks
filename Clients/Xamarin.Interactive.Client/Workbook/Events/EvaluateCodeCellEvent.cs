//
// EvaluateCodeCellEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

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