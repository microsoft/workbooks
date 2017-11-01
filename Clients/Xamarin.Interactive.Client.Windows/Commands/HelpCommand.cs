//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Xamarin.Interactive.Client.Windows.Commands
{
    class HelpCommand : ICommand
    {
        public bool CanExecute (object parameter)
        {
            return true;
        }

        public void Execute (object parameter)
        {
            Process.Start (ClientInfo.HelpUri.ToString ());
        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67
    }
}
