//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Collections;

namespace Xamarin.Interactive
{
    class ObserverCollection<T> : IObserver<T>
    {
        readonly Func<IObserver<T>, IDisposable> subscribeHandler;
        ImmutableList<IObserver<T>> observers = ImmutableList<IObserver<T>>.Empty;

        public ObserverCollection (Func<IObserver<T>, IDisposable> subscribeHandler)
        {
            this.subscribeHandler = subscribeHandler;
        }

        public IDisposable Subscribe (IObserver<T> observer)
        {
            var subscription = subscribeHandler (observer);
            observers = observers.Add (observer);
            return subscription;
        }

        public void Remove (IObserver<T> observer)
            => observers = observers.Remove (observer);

        public void OnNext (T value)
            => observers.ForEach (o => o.OnNext (value));

        public void OnError (Exception error)
        {
            observers.ForEach (o => o.OnError (error));
            observers = ImmutableList<IObserver<T>>.Empty;
        }

        public void OnCompleted ()
        {
            observers.ForEach (o => o.OnCompleted ());
            observers = ImmutableList<IObserver<T>>.Empty;
        }
    }
}