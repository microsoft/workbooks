//
// NSUserDefaultsPreferenceStore.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Immutable;

using Foundation;

namespace Xamarin.Interactive.Preferences
{
	sealed class NSUserDefaultsPreferenceStore : IPreferenceStore
	{
		readonly Observable<PreferenceChange> observable = new Observable<PreferenceChange> ();
		readonly NSUserDefaults userDefaults;
		readonly string bundleId;
		readonly bool synchronizeOnUpdate;

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

			this.userDefaults = userDefaults;
			this.bundleId = bundle.BundleIdentifier;
			this.synchronizeOnUpdate = synchronizeOnSet;
		}

		string MakeKey (string key) => $"{bundleId}.{key}";

		void Set (string key, Action setter)
		{
			setter ();
			if (synchronizeOnUpdate)
				userDefaults.Synchronize ();
			observable.Observers.OnNext (new PreferenceChange (key));
		}

		public void Set (string key, bool value)
			=> Set (key, () => userDefaults.SetBool (value, MakeKey (key)));

		public void Set (string key, long value)
			=> Set (key, () => userDefaults.SetInt ((nint)value, MakeKey (key)));

		public void Set (string key, double value)
			=> Set (key, () => userDefaults.SetDouble (value, MakeKey (key)));

		public void Set (string key, string value)
			=> Set (key, () => userDefaults.SetString (value, MakeKey (key)));

		public void Set (string key, string [] value)
			=> Set (key, () => {
				using (var strings = NSArray.FromStrings (value))
					userDefaults [MakeKey (key)] = strings;
			});

		public bool GetBoolean (string key, bool defaultValue = false)
			=> userDefaults [key = MakeKey (key)] == null ? defaultValue : userDefaults.BoolForKey (key);

		public double GetDouble (string key, double defaultValue = 0.0)
			=> userDefaults [key = MakeKey (key)] == null ? defaultValue : userDefaults.DoubleForKey (key);

		public long GetInt64 (string key, long defaultValue = 0)
			=> userDefaults [key = MakeKey (key)] == null ? defaultValue : userDefaults.IntForKey (key);

		public string GetString (string key, string defaultValue = null)
			=> userDefaults [key = MakeKey (key)] == null ? defaultValue : userDefaults.StringForKey (key);

		public string [] GetStringArray (string key, string [] defaultValue = null)
			=> userDefaults [key = MakeKey (key)] == null
				? defaultValue
				: userDefaults.StringArrayForKey (key);

		public void Remove (string key)
		{
			userDefaults.RemoveObject (MakeKey (key));
			if (synchronizeOnUpdate)
				userDefaults.Synchronize ();
			observable.Observers.OnNext (new PreferenceChange (key));
		}

		public void RemoveAll ()
		{
			var toNotify = Keys;

			foreach (var key in toNotify)
				userDefaults.RemoveObject (key);

			if (synchronizeOnUpdate)
				userDefaults.Synchronize ();

			foreach (var key in toNotify)
				observable.Observers.OnNext (new PreferenceChange (key));
		}

		public ImmutableList<string> Keys {
			get {
				var keys = ImmutableList<string>.Empty;

				foreach (var nsKey in userDefaults.ToDictionary ().Keys) {
					var key = nsKey?.ToString ();
					if (key != null && key.StartsWith ($"{bundleId}.", StringComparison.Ordinal)) {
						userDefaults.RemoveObject (key);
						keys = keys.Add (key.Substring (bundleId.Length + 1));
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