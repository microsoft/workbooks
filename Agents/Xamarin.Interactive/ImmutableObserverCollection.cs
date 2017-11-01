//
// ObserverCollection.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using Xamarin.Interactive.Collections;

namespace Xamarin.Interactive
{
	public struct ImmutableObserverCollection<T> : IObserver<T>
	{
		public static ImmutableObserverCollection<T> Create (Func<IObserver<T>, IDisposable> subscribeHandler)
		{
			if (subscribeHandler == null)
				throw new ArgumentNullException (nameof (subscribeHandler));

			return new ImmutableObserverCollection<T> {
				subscribeHandler = subscribeHandler,
				observers = ImmutableList<IObserver<T>>.Empty
			};
		}

		Func<IObserver<T>, IDisposable> subscribeHandler;
		ImmutableList<IObserver<T>> observers;

		public ImmutableObserverCollection<T> Subscribe (IObserver<T> observer)
		{
			IDisposable subscription;
			return Subscribe (observer, out subscription);
		}

		public ImmutableObserverCollection<T> Subscribe (IObserver<T> observer, out IDisposable subscription)
		{
			subscription = subscribeHandler (observer);
			return new ImmutableObserverCollection<T> {
				subscribeHandler = subscribeHandler,
				observers = observers.Add (observer)
			};
		}

		public ImmutableObserverCollection<T> Remove (IObserver<T> observer) =>
			new ImmutableObserverCollection<T> {
				subscribeHandler = subscribeHandler,
				observers = observers.Remove (observer)
			};

		public void OnNext (T value) => observers.ForEach (o => o.OnNext (value));
		public void OnError (Exception error) => observers.ForEach (o => o.OnError (error));
		public void OnCompleted () => observers.ForEach (o => o.OnCompleted ());
	}
}