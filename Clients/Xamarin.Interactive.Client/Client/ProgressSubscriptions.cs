//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;

namespace Xamarin.Interactive.Client
{
    /// <summary>
	/// Track concurrent actions that determine whether a UI should show a progress state.
	/// </summary>
    class ProgressSubscriptions
    {
        ImmutableList<IDisposable> subscriptions = ImmutableList<IDisposable>.Empty;

        public event EventHandler Changed;

        public bool ShowProgress => !subscriptions.IsEmpty;

        class ProgressSubscription : IDisposable
        {
            readonly ProgressSubscriptions subscriptions;

            public ProgressSubscription (ProgressSubscriptions subscriptions)
            {
                this.subscriptions = subscriptions;
            }

            void IDisposable.Dispose ()
            {
                subscriptions.subscriptions = subscriptions.subscriptions.Remove (this);
                subscriptions.Changed?.Invoke (subscriptions, EventArgs.Empty);
            }
        }

        public IDisposable Subscribe ()
        {
            var subscription = new ProgressSubscription (this);
            subscriptions = subscriptions.Add (subscription);
            Changed?.Invoke (this, EventArgs.Empty);
            return subscription;
        }
    }
}
