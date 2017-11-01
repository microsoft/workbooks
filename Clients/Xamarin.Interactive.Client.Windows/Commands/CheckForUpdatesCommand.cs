// CheckForUpdatesCommand.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.

using System;
using System.Windows;
using System.Windows.Input;

namespace Xamarin.Interactive.Client.Windows.Commands
{
    sealed class CheckForUpdatesCommand : ICommand
    {
        public bool CanExecute (object parameter)
            => true;

        public void Execute (object parameter)
            => App.CheckForUpdatesInBackground (
                parameter as Window,
                userInitiated: true);

        #pragma warning disable 67
        public event EventHandler CanExecuteChanged;
        #pragma warning restore 67
    }
}