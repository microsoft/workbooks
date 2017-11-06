//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

using Xamarin.Interactive.Workbook.Models;
using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Client.Console
{
    sealed class ConsoleWorkbookPageViewModel : WorkbookPageViewModel
    {
        readonly TextWriter output = System.Console.Out;

        public ConsoleWorkbookPageViewModel (ClientSession clientSession, WorkbookPage workbookPage)
            : base (clientSession, workbookPage)
        {
        }

        protected override void BindCodeCellToView (CodeCell cell, CodeCellState codeCellState)
        {
            var view = new ConsoleCellView (cell, output);
            codeCellState.Editor = view.Editor;
            codeCellState.View = view;
            cell.View = view;
        }

        protected override void BindMarkdownCellToView (MarkdownCell cell)
            => cell.View = new ConsoleCellView (cell, output);

        protected override void InsertCellInViewModel (Cell newCell, Cell previousCell)
        {
        }

        protected override void UnbindCellFromView (ICellView cellView)
        {
        }
    }
}