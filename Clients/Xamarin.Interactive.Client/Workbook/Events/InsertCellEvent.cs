//
// InsertCellEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Events
{
    abstract class InsertCellEvent : EditorEvent
    {
        public Cell SourceCell { get; }

        protected InsertCellEvent (Cell sourceCell) : base (sourceCell.View.Editor)
            => SourceCell = sourceCell;
    }

    sealed class InsertCellEvent<TCell> : InsertCellEvent
        where TCell : Cell
    {
        public InsertCellEvent (Cell sourceCell) : base (sourceCell)
        {
        }
    }
}