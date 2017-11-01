//
// MarkdownCell.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using CommonMark;
using CommonMark.Syntax;

using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Workbook.Models
{
	sealed class MarkdownCell : Cell
	{
		sealed class MarkdownBuffer : ICellBuffer
		{
			readonly MarkdownCell markdownCell;

			public MarkdownBuffer (MarkdownCell markdownCell)
				=> this.markdownCell = markdownCell;

			string value;

			public string Value {
				get {
					var view = markdownCell.View as MarkdownCellView;
					return view == null ? value : view.MarkdownContent;
				}

				set {
					var view = markdownCell.View as MarkdownCellView;
					if (view == null)
						this.value = value;
					else
						view.MarkdownContent = value;
				}
			}

			public int Length {
				get {
					var value = Value;
					return value == null ? 0 : value.Length;
				}
			}
		}

		public override ICellBuffer Buffer { get; }

		public MarkdownCell (Block markdownBlock = null)
		{
			Buffer = new MarkdownBuffer (this);
			Buffer.Value = markdownBlock?.ToMarkdownString ();
		}

		public override Block ToMarkdownDocumentBlock ()
			=> CommonMarkConverter.Parse (
				Buffer.Value,
				WorkbookDocument.CommonMarkSettings);
	}
}