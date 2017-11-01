// ShowOptionsCommand.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.

using System;
using System.Windows;
using System.Windows.Input;

using Xamarin.Interactive.Client.Windows.Views;

namespace Xamarin.Interactive.Client.Windows.Commands
{
	class ShowOptionsCommand : ICommand
	{
		OptionsWindow optionsWindow;

		public bool CanExecute (object parameter)
		{
			return true;
		}

		public void Execute (object parameter)
		{
			if (optionsWindow == null) {
				optionsWindow = new OptionsWindow ();
				optionsWindow.Closed += (o, e) => optionsWindow = null;
			}

			if (parameter is OptionsWindow.Tab)
				optionsWindow.SelectTab ((OptionsWindow.Tab)parameter);

			optionsWindow.Show ();
			optionsWindow.Activate ();
		}

#pragma warning disable 67
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67
	}
}
