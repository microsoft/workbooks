// OpenFileCommand.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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