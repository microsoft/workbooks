//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Workbook.LoadAndSave
{
    interface IWorkbookSaveOperation
    {
        WorkbookSaveOptions SupportedOptions { get; }
        WorkbookSaveOptions Options { get; set; }
        PackageReferenceList PackageReferences { get; set; }
        FilePath Destination { get; set; }
    }
}