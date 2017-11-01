//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Xamarin.Interactive.Workbook.Structure
{
    interface IWorkbookTitledNode : INotifyPropertyChanged
    {
        string Title { get; set; }
    }
}