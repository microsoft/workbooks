//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Workbook.LoadAndSave
{
    interface IWorkbookSaveOperation
    {
        bool SupportsMultiFileOptions { get; }
        bool SupportsSingleFileOption { get; }
        bool InvolesMultipleFiles { get; }
        WorkbookSaveOptions SaveOption { get; set; }
        FilePath Destination { get; set; }
    }
}