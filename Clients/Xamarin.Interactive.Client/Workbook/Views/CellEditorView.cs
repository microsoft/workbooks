//
// CellEditorView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Views
{
	abstract class CellEditorView : AbstractEditor
	{
		public abstract Cell Cell { get; }
	}
}