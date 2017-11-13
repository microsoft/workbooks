//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.Events;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Events
{
    abstract class DeleteCellEvent : EditorEvent, IDocumentDirtyEvent
    {
        public Cell SourceCell { get; }

        protected DeleteCellEvent (Cell sourceCell) : base (sourceCell.View.Editor)
            => SourceCell = sourceCell;
    }

    sealed class DeleteCellEvent<TCell> : DeleteCellEvent
        where TCell : Cell
    {
        public DeleteCellEvent (Cell sourceCell) : base (sourceCell)
        {
        }
    }
}