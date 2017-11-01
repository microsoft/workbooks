//
// ICellBuffer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Workbook.Models
{
    interface ICellBuffer
    {
        string Value { get; set; }
        int Length { get; }
    }
}