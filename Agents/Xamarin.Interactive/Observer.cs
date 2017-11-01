//
// Observer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive
{
	class Observer<T> : IObserver<T>
	{
		readonly Action<T> nextHandler;
		readonly Action<Exception> errorHandler;
		readonly Action completedHandler;

		public Observer (Action<T> nextHandler,
			Action<Exception> errorHandler = null,
			Action completedHandler = null)
		{
			if (nextHandler == null)
				throw new ArgumentNullException (nameof (nextHandler));

			this.nextHandler = nextHandler;
			this.errorHandler = errorHandler;
			this.completedHandler = completedHandler;
		}

		public void OnNext (T value) => nextHandler.Invoke (value);
		public void OnError (Exception error) => errorHandler?.Invoke (error);
		public void OnCompleted () => completedHandler?.Invoke ();
	}
}