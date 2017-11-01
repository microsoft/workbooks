//
// ICellView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using Xamarin.Interactive.Editor;

namespace Xamarin.Interactive.Workbook.Views
{
	interface ICellView
	{
		IEditor Editor { get; }
		void Focus (bool scrollIntoView = true);
	}
}