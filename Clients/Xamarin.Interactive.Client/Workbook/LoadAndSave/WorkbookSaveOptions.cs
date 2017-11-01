//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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