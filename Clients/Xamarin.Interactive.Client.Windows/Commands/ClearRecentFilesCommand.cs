// ClearRecentFilesCommand.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Windows.Input;

namespace Xamarin.Interactive.Client.Windows.Commands
{
	sealed class ClearRecentFilesCommand : ICommand
	{
		#pragma warning disable 67
		public event EventHandler CanExecuteChanged;
		#pragma warning restore 67

		public bool CanExecute (object parameter) => true;

		public void Execute (object parameter)
			=> App.RecentDocuments.Clear ();
	}
}