// AboutCommand.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using System;
using System.Windows;
using System.Windows.Input;

using Xamarin.Interactive.Client.Windows.Views;

namespace Xamarin.Interactive.Client.Windows.Commands
{
	class AboutCommand : ICommand
	{
		public bool CanExecute (object parameter)
		{
			return true;
		}

		public void Execute (object parameter)
		{
			var parent = parameter as Window;
			if (parent == null)
				return;

			new AboutWindow { Owner = parent }.ShowDialog ();
		}

#pragma warning disable 67
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67
	}
}
