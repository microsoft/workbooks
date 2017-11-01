//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive
{
    sealed class Observable<T> : IObservable<T>
    {
        ImmutableObserverCollection<T> observers;

        public IObserver<T> Observers => observers;

        class Subscription : IDisposable
        {
            readonly Observable<T> observable;
            readonly IObserver<T> observer;

            public Subscription (Observable<T> observable, IObserver<T> observer)
            {
                this.observable = observable;
                this.observer = observer;
            }

            public void Dispose ()
            {
                observable.observers = observable.observers.Remove (observer);
            }
        }

        public Observable (IObserver<T> dispatcher = null)
        {
            observers = ImmutableObserverCollection<T>.Create (
                observer => new Subscription (this, observer));
        }

        public IDisposable Subscribe (IObserver<T> observer)
        {
            IDisposable subscription;
            observers = observers.Subscribe (observer, out subscription);
            return subscription;
        }
    }
}