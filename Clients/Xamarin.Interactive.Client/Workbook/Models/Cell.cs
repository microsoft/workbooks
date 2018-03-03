//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CommonMark.Syntax;

using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Workbook.Models
{
    abstract class Cell
    {
        public WorkbookDocument Document { get; internal set; }
        public Cell PreviousCell { get; internal set; }
        public Cell NextCell { get; internal set; }

        public bool ShouldSerialize { get; protected set; } = true;

        public abstract ICellBuffer Buffer { get; }
        public ICellView View { get; set; }

        public abstract Block ToMarkdownDocumentBlock ();

        public TCell GetNextCell<TCell> () where TCell : Cell
            => GetNextCell<TCell> (NextCell);

        public TCell GetSelfOrNextCell<TCell> () where TCell : Cell
            => GetNextCell<TCell> (this);

        TCell GetNextCell<TCell> (Cell startCell) where TCell : Cell
        {
            var cell = startCell;
            while (cell != null) {
                if (cell is TCell tCell)
                    return tCell;
                cell = cell.NextCell;
            }
            return null;
        }

        public TCell GetPreviousCell<TCell> () where TCell : Cell
            => GetPreviousCell<TCell> (PreviousCell);

        public TCell GetSelfOrPreviousCell<TCell> () where TCell : Cell
            => GetPreviousCell<TCell> (this);

        TCell GetPreviousCell<TCell> (Cell startCell) where TCell : Cell
        {
            var cell = startCell;
            while (cell != null) {
                if (cell is TCell tCell)
                    return tCell;
                cell = cell.PreviousCell;
            }
            return null;
        }

        protected Block ToMarkdownDocumentBlock (BlockTag blockTag, FencedCodeData fencedCodeData)
        {
            var document = new Block (BlockTag.Document, 0);

            var block = new Block (blockTag, 0) {
                Parent = document,
                Top = document,
                FencedCodeData = fencedCodeData,
                StringContent = new StringContent ()
            };

            if (Buffer.Length > 0) {
                block.StringContent.Append (Buffer.Value, 0, Buffer.Length);
                if (Buffer.Value [Buffer.Length - 1] != '\n')
                    block.StringContent.Append ("\n", 0, 1);
            }

            document.FirstChild = block;

            return document;
        }
    }
}