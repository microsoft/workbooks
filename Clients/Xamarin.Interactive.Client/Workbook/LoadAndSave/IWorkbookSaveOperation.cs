//
// IWorkbookSaveInfo.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Workbook.LoadAndSave
{
	interface IWorkbookSaveOperation
	{
		WorkbookSaveOptions SupportedOptions { get; }
		WorkbookSaveOptions Options { get; set; }
		FilePath Destination { get; set; }
	}
}