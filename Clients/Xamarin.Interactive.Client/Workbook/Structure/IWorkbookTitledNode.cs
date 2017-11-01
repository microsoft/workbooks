//
// IWorkbookTitledNode.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.ComponentModel;

namespace Xamarin.Interactive.Workbook.Structure
{
    interface IWorkbookTitledNode : INotifyPropertyChanged
    {
        string Title { get; set; }
    }
}