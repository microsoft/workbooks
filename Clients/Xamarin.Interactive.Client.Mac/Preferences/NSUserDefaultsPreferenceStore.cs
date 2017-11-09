//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;

using Foundation;

namespace Xamarin.Interactive.Preferences
{
    sealed class NSUserDefaultsPreferenceStore : IPreferenceStore
    {
        readonly Observable<PreferenceChange> observable = new Observable<PreferenceChange> ();
        readonly bool synchronizeOnUpdate;

        public NSUserDefaults UserDefaults { get; }
        public string BundleId { get; }

        public NSUserDefaultsPreferenceStore (bool synchronizeOnSet = false)
            : this (NSUserDefaults.StandardUserDefaults, NSBundle.MainBundle, synchronizeOnSet)
        {
        }

        public NSUserDefaultsPreferenceStore (
            NSUserDefaults userDefaults,
            NSBundle bundle,
            bool synchronizeOnSet = false)
        {
            if (userDefaults == null)
                throw new ArgumentNullException (nameof (userDefaults));

            if (bundle == null)
                throw new ArgumentNullException (nameof (bundle));

            this.UserDefaults = userDefaults;
            this.BundleId = bundle.BundleIdentifier;
            this.synchronizeOnUpdate = synchronizeOnSet;
        }

        string MakeKey (string key) => $"{BundleId}.{key}";

        void Set (string key, Action setter)
        {
            setter ();
            if (synchronizeOnUpdate)
                UserDefaults.Synchronize ();
            observable.Observers.OnNext (new PreferenceChange (key));
        }

        public void Set (string key, bool value)
            => Set (key, () => UserDefaults.SetBool (value, MakeKey (key)));

        public void Set (string key, long value)
            => Set (key, () => UserDefaults.SetInt ((nint)value, MakeKey (key)));

        public void Set (string key, double value)
            => Set (key, () => UserDefaults.SetDouble (value, MakeKey (key)));

        public void Set (string key, string value)
            => Set (key, () => UserDefaults.SetString (value, MakeKey (key)));

        public void Set (string key, string [] value)
            => Set (key, () => {
                using (var strings = NSArray.FromStrings (value))
                    UserDefaults [MakeKey (key)] = strings;
            });

        public bool GetBoolean (string key, bool defaultValue = false)
            => UserDefaults [key = MakeKey (key)] == null ? defaultValue : UserDefaults.BoolForKey (key);

        public double GetDouble (string key, double defaultValue = 0.0)
            => UserDefaults [key = MakeKey (key)] == null ? defaultValue : UserDefaults.DoubleForKey (key);

        public long GetInt64 (string key, long defaultValue = 0)
            => UserDefaults [key = MakeKey (key)] == null ? defaultValue : UserDefaults.IntForKey (key);

        public string GetString (string key, string defaultValue = null)
            => UserDefaults [key = MakeKey (key)] == null ? defaultValue : UserDefaults.StringForKey (key);

        public string [] GetStringArray (string key, string [] defaultValue = null)
            => UserDefaults [key = MakeKey (key)] == null
                ? defaultValue
                : UserDefaults.StringArrayForKey (key);

        public void Remove (string key)
        {
            UserDefaults.RemoveObject (MakeKey (key));
            if (synchronizeOnUpdate)
                UserDefaults.Synchronize ();
            observable.Observers.OnNext (new PreferenceChange (key));
        }

        public void RemoveAll ()
        {
            var toNotify = Keys;

            foreach (var key in toNotify)
                UserDefaults.RemoveObject (key);

            if (synchronizeOnUpdate)
                UserDefaults.Synchronize ();

            foreach (var key in toNotify)
                observable.Observers.OnNext (new PreferenceChange (key));
        }

        public ImmutableList<string> Keys {
            get {
                var keys = ImmutableList<string>.Empty;

                foreach (var nsKey in UserDefaults.ToDictionary ().Keys) {
                    var key = nsKey?.ToString ();
                    if (key != null && key.StartsWith ($"{BundleId}.", StringComparison.Ordinal)) {
                        UserDefaults.RemoveObject (key);
                        keys = keys.Add (key.Substring (BundleId.Length + 1));
                    }
                }

                return keys;
            }
        }

        IDisposable IObservable<PreferenceChange>.Subscribe (IObserver<PreferenceChange> observer)
            => observable.Subscribe (observer);

        public IDisposable Subscribe (Action<PreferenceChange> observer)
            => observable.Subscribe (new Observer<PreferenceChange> (observer));
    }
}