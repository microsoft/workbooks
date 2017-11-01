//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows.Input;

namespace Xamarin.Interactive.Client.Windows.Commands
{
    sealed class OpenFileCommand : ICommand
    {
        #pragma warning disable 67
        public event EventHandler CanExecuteChanged;
        #pragma warning restore 67

        public bool CanExecute (object parameter) => true;

        public void Execute (object parameter)
            => App.OpenWorkbook (new Uri (parameter.ToString ()));
    }
}