// DelegateCommand.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Windows.Input;

namespace Xamarin.Interactive.Client.Windows.Commands
{
	class DelegateCommand : ICommand
	{
		readonly Action<object> execute;
		readonly Predicate<object> canExecute;
			
		public DelegateCommand (Action<object> action, Predicate<object> predicate) : this (action)
			=> canExecute = predicate ?? throw new ArgumentNullException (nameof (predicate));

		public DelegateCommand (Action<object> action) 
			=> execute = action ?? throw new ArgumentNullException (nameof (action));

		public event EventHandler CanExecuteChanged;

		public void InvalidateCanExecute ()
			=> CanExecuteChanged?.Invoke (this, new EventArgs ());

		public bool CanExecute (object parameter)
			=> canExecute?.Invoke (parameter) ?? true;

		public void Execute (object parameter)
			=> execute.Invoke (parameter);
	}
}
