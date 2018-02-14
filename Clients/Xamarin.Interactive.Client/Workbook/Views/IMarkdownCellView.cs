//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Workbook.Views
{
    interface IMarkdownCellView
    {
        string MarkdownContent { get; set; }
    }
}