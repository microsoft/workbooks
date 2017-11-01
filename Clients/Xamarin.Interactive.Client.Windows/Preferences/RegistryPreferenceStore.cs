//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Globalization;

using Microsoft.Win32;

namespace Xamarin.Interactive.Preferences
{
    sealed class RegistryPreferenceStore : IPreferenceStore
    {
        readonly Observable<PreferenceChange> observable = new Observable<PreferenceChange> ();
        readonly RegistryHive hive;
        readonly RegistryView view;
        readonly string prefsSubKey;

        public RegistryPreferenceStore (RegistryHive hive, RegistryView view, string prefsSubKey)
        {
            this.hive = hive;
            this.view = view;
            this.prefsSubKey = prefsSubKey;
        }

        RegistryKey GetPrefsBaseKey () => RegistryKey.OpenBaseKey (hive, view);

        RegistryKey GetPrefsKey (bool writable = false)
            => writable
            ? GetPrefsBaseKey ().CreateSubKey (prefsSubKey, writable: true)
            : GetPrefsBaseKey ().OpenSubKey (prefsSubKey);

        RegistryKey GetSubKey (KeyPath keyPath, bool writable = false)
        {
            var prefsKey = GetPrefsKey (writable);

            if (String.IsNullOrEmpty (keyPath.SubKey))
                return prefsKey;

            return writable
                ? prefsKey?.CreateSubKey (keyPath.SubKey, writable: true)
                : prefsKey?.OpenSubKey (keyPath.SubKey);
        }

        struct KeyPath
        {
            public string SubKey { get; }
            public string Name { get; }

            public KeyPath (string prefKey)
            {
                var split = prefKey.LastIndexOf ('.');
                Name = prefKey.Substring (split + 1);
                if (split >= 0)
                    SubKey = prefKey.Substring (0, split).Replace ('.', '\\');
                else
                    SubKey = null;
            }
        }

        object Get (string key, object defaultValue)
        {
            var keyPath = new KeyPath (key);
            using (var registryKey = GetSubKey (keyPath))
                return registryKey?.GetValue (keyPath.Name, defaultValue) ?? defaultValue;
        }

        void Set (string key, object value, RegistryValueKind kind)
        {
            var keyPath = new KeyPath (key);
            using (var registryKey = GetSubKey (keyPath, writable: true))
                registryKey.SetValue (keyPath.Name, value, kind);
            observable.Observers.OnNext (new PreferenceChange (key));
        }

        public void Set (string key, bool value)
            => Set (key, value ? 1 : 0, RegistryValueKind.DWord);

        public void Set (string key, long value)
            => Set (key, value, RegistryValueKind.QWord);

        public void Set (string key, double value)
            => Set (key, value.ToString ("R", CultureInfo.InvariantCulture), RegistryValueKind.String);

        public void Set (string key, string value)
            => Set (key, value, RegistryValueKind.String);

        public void Set (string key, string[] value)
            => Set (key, value, RegistryValueKind.MultiString);

        public bool GetBoolean (string key, bool defaultValue = false)
        {
            var intVal = (int) Get (key, defaultValue ? 1 : 0);
            return intVal == 1;
        }

        public long GetInt64 (string key, long defaultValue = 0)
            => (long) Get (key, defaultValue);

        public double GetDouble (string key, double defaultValue = 0)
        {
            var stringVal = Get (key, defaultValue.ToString ()) as string;
            double val;
            if (stringVal == null || !Double.TryParse (stringVal, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                return defaultValue;
            return val;
        }

        public string GetString (string key, string defaultValue = null)
            => Get (key, defaultValue) as string;

        public string[] GetStringArray (string key, string[] defaultValue = null)
            => Get (key, defaultValue) as string[];

        public void Remove (string key)
        {
            var keyPath = new KeyPath (key);
            using (var registryKey = GetSubKey (keyPath, writable: true)) {
                try {
                    registryKey?.DeleteValue (keyPath.Name);
                } catch (ArgumentException) {
                    // Expected if sub key doesn't actually exist yet
                }
            }
            observable.Observers.OnNext (new PreferenceChange (key));
        }

        public void RemoveAll ()
        {
            var deletingPrefs = Keys;
            using (var baseKey = GetPrefsBaseKey ()) {
                try {
                    baseKey.DeleteSubKeyTree (prefsSubKey);
                } catch (ArgumentException) {
                    // Expected if sub key doesn't actually exist yet
                }
            }
            deletingPrefs.ForEach (p => observable.Observers.OnNext (new PreferenceChange (p)));
        }

        public ImmutableList<string> Keys {
            get {
                using (var prefsKey = GetPrefsKey ()) {
                    return GetPrefKeys (prefsKey, null);
                }
            }
        }

        ImmutableList<string> GetPrefKeys (RegistryKey key, string basePath)
        {
            var keys = ImmutableList<string>.Empty;
            if (key == null)
                return keys;

            foreach (var name in key.GetValueNames ()) {
                var prefKey = name;
                if (!String.IsNullOrEmpty (basePath))
                    prefKey = $"{basePath}.{name}";
                keys = keys.Add (prefKey);
            }

            foreach (var subKey in key.GetSubKeyNames ()) {
                keys = keys.AddRange (GetPrefKeys (
                    key.OpenSubKey (subKey),
                    String.IsNullOrEmpty (basePath) ? subKey : $"{basePath}.{subKey}"));
            }

            return keys;
        }

        public IDisposable Subscribe (IObserver<PreferenceChange> observer)
            => observable.Subscribe (observer);

        public IDisposable Subscribe (Action<PreferenceChange> nextHandler)
            => observable.Subscribe (new Observer<PreferenceChange> (nextHandler));
    }
}
