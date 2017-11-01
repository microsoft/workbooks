//
// Theme.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Immutable;

using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Client
{
	abstract class Theme
	{
		static readonly Observable<Theme> change = new Observable<Theme> ();
		public static IObservable<Theme> Change => change;

		public static readonly Theme Light = new LightTheme ();
		public static readonly Theme Dark = new DarkTheme ();

		static Theme current;
		public static Theme Current {
			get => current;
			private set {
				if (current != value) {
					current = value;
					change.Observers.OnNext (Current);
				}
			}
		}

		static Theme ()
		{
			SetTheme ();
			PreferenceStore.Default.Subscribe (OnPreferenceChange);
		}

		static void OnPreferenceChange (PreferenceChange change)
		{
			if (change.Key == Prefs.UI.Theme.UseHighContrast.Key ||
				change.Key == Prefs.UI.Theme.ThemeName.Key)
				SetTheme ();
		}

		static void SetTheme ()
		{
			if (Prefs.UI.Theme.UseHighContrast.GetValue ()) {
				Current = Dark;
				return;
			}

			switch (Prefs.UI.Theme.ThemeName.GetValue ()?.ToLowerInvariant ()) {
			case "dark":
				Current = Dark;
				break;
			case "light":
			default:
				Current = Light;
				break;
			}
		}

		ImmutableDictionary<string, object> itemCache = ImmutableDictionary<string, object>.Empty;

		protected Theme ()
		{
		}

		protected abstract string IconModifier { get; }

		public string GetIconName (string name, int size, bool selected = false)
			=> $"{name}-{size}{IconModifier}{(selected ? "~sel" : "")}";

		public bool TryGetCachedItem<T> (string cacheKey, out T item)
		{
			if (itemCache.TryGetValue (cacheKey, out var _item)) {
				item = (T)_item;
				return true;
			}

			item = default (T);
			return false;
		}

		public void CacheItem (string cacheKey, object item)
		{
			if (item != null)
				itemCache = itemCache.Add (cacheKey, item);
		}

		sealed class LightTheme : Theme
		{
			protected override string IconModifier => null;
		}

		sealed class DarkTheme : Theme
		{
			protected override string IconModifier => "~dark";
		}
	}
}