// CloseWindowCommand.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Windows;
using System.Windows.Input;

namespace Xamarin.Interactive.Client.Windows.Commands
{
	class CloseWindowCommand : ICommand
	{
		#pragma warning disable 67
		public event EventHandler CanExecuteChanged;
		#pragma warning restore 67

		public bool CanExecute (object parameter)
			=> parameter is Window;

		public void Execute (object parameter)
			=> (parameter as Window)?.Close ();
	}
}
