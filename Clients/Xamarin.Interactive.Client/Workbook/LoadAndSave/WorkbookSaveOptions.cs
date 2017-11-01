//
// WorkbookSaveOptions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Workbook.LoadAndSave
{
	[Flags]
	enum WorkbookSaveOptions
	{
		None = 0,
		Archive = 1 << 0,
		Sign = 1 << 1
	}
}