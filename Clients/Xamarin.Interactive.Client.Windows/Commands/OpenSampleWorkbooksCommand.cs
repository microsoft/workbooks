//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Xamarin.Interactive.Client.Windows.Commands
{
    public class OpenSampleWorkbooksCommand : ICommand
    {
#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute (object parameter) => true;

        public void Execute (object parameter) => Process.Start (ClientInfo.DownloadWorkbooksUri.ToString ());
    }
}