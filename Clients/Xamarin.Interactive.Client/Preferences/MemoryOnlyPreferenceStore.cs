//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;

namespace Xamarin.Interactive.Preferences
{
    sealed class MemoryOnlyPreferenceStore : IPreferenceStore
    {
        readonly Observable<PreferenceChange> observable = new Observable<PreferenceChange> ();

        ImmutableDictionary<string, object> store = ImmutableDictionary<string, object>.Empty;

        void Set (string key, object value)
        {
            store = store.SetItem (key, value);
            observable.Observers.OnNext (new PreferenceChange (key));
        }

        public void Set (string key, bool value) => Set (key, (object)value);
        public void Set (string key, long value) => Set (key, (object)value);
        public void Set (string key, double value) => Set (key, (object)value);
        public void Set (string key, string value) => Set (key, (object)value);
        public void Set (string key, string [] value) => Set (key, (object)value);

        public bool GetBoolean (string key, bool defaultValue = false)
            => store.TryGetValue (key, out var value) ? (bool)value : defaultValue;

        public double GetDouble (string key, double defaultValue = 0.0)
            => store.TryGetValue (key, out var value) ? (double)value : defaultValue;

        public long GetInt64 (string key, long defaultValue = 0)
            => store.TryGetValue (key, out var value) ? (long)value : defaultValue;

        public string GetString (string key, string defaultValue = null)
            => store.TryGetValue (key, out var value) ? (string)value : defaultValue;

        public string [] GetStringArray (string key, string [] defaultValue = null)
            => store.TryGetValue (key, out var value) ? (string [])value : defaultValue;

        public void Remove (string key)
        {
            store = store.Remove (key);
            observable.Observers.OnNext (new PreferenceChange (key));
        }

        public void RemoveAll ()
            => store.Keys.ToImmutableArray ().ForEach (Remove);

        public ImmutableList<string> Keys
            => store.Keys.ToImmutableList ();

        IDisposable IObservable<PreferenceChange>.Subscribe (IObserver<PreferenceChange> observer)
            => observable.Subscribe (observer);

        public IDisposable Subscribe (Action<PreferenceChange> observer)
            => observable.Subscribe (new Observer<PreferenceChange> (observer));
    }
}