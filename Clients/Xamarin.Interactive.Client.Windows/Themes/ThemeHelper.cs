using System;
using System.ComponentModel;
using System.Windows;

using MahApps.Metro;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Client.Windows.Themes
{
	static class ThemeHelper
	{
		public const string HighContrastThemeName = "High Contrast";

		public static readonly string [] Themes = {
			"Light",
			"Dark",
			HighContrastThemeName
		};

		static readonly string ThemeUriBase = $"pack://application:,,,/{ClientInfo.FullProductName}" +
			";component/Themes/{0}.xaml";

		static IDisposable preferenceSubscription;

		public static ResourceDictionary GetThemeResourceDictionary (string themeName)
			=> new ResourceDictionary { Source = GetUriForTheme (themeName) };

		static Uri GetUriForTheme (string themeName)
			=> new Uri (string.Format (ThemeUriBase, themeName));

		public static void Initialize (Application app)
		{
			Themes.ForEach (theme => ThemeManager.AddAppTheme (theme, GetUriForTheme (theme)));
			Themes.ForEach (theme => ThemeManager.AddAccent (theme, GetUriForTheme (theme)));

			preferenceSubscription = PreferenceStore.Default.Subscribe (ObservePreferenceChange);
			SystemParameters.StaticPropertyChanged += DetectHighContrastProperty;

			Prefs.UI.Theme.UseHighContrast.SetValue (SystemParameters.HighContrast);

			var themeToLoad = Prefs.UI.Theme.UseHighContrast.GetValue ()
				? HighContrastThemeName
				: Prefs.UI.Theme.ThemeName.GetValue ();

			// MahApps' theme-switching code doesn't work w/o a theme already loaded, so
			// load the right one from prefs by merging the resource dictionary in by hand.
			app.Resources.MergedDictionaries.Add (GetThemeResourceDictionary (themeToLoad));
		}

		static void DetectHighContrastProperty (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (SystemParameters.HighContrast)) {
				// Set UseHighContrast for notification/routing of high contrast.
				Prefs.UI.Theme.UseHighContrast.SetValue (SystemParameters.HighContrast);

				// But don't change the actual theme pref, just change the theme, so when
				// high contrast is turned off, we get the user's chosen theme again.
				var newTheme = SystemParameters.HighContrast ? HighContrastThemeName : Prefs.UI.Theme.ThemeName.GetValue ();
				TryChangeTheme (newTheme);
			}
		}

		static void ObservePreferenceChange (PreferenceChange change)
		{
			if (change.Key == Prefs.UI.Theme.ThemeName.Key)
				TryChangeTheme (Prefs.UI.Theme.ThemeName.GetValue ());
		}

		static void TryChangeTheme (string newTheme)
		{
			try {
				ThemeManager.ChangeAppStyle (
					Application.Current,
					ThemeManager.GetAccent (newTheme),
					ThemeManager.GetAppTheme (newTheme));
			} catch (Exception e) {
				Log.Warning (nameof (ThemeHelper), "Could not change theme.", e);
			}
		}
	}
}
