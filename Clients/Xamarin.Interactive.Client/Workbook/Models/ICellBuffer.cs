//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Workbook.Models
{
    interface ICellBuffer
    {
        string Value { get; set; }
        int Length { get; }
    }
}