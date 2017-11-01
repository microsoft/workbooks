//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Editor;

namespace Xamarin.Interactive.Workbook.Views
{
    interface ICellView
    {
        IEditor Editor { get; }
        void Focus (bool scrollIntoView = true);
    }
}