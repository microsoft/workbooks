//
// PreferencesDeveloperViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Preferences
{
	sealed partial class PreferencesDeveloperViewController : PreferencesViewController
	{
		PreferencesDeveloperViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			foreach (var paneName in Prefs.Developer.WebInspectorPaneNames.All)
				inspectorPanePopupButton.AddItem (paneName);

			inspectorPanePopupButton.Activated += (sender, e) =>
				Prefs.Developer.StartupWebInspectorPane.SetValue (
					(Prefs.Developer.WebInspectorPane)(int)
						inspectorPanePopupButton.IndexOfSelectedItem);

			ObservePreferenceChange (new PreferenceChange (Prefs.Developer.StartupWebInspectorPane.Key));
		}

		protected override void ObservePreferenceChange (PreferenceChange change)
		{
			if (change.Key == Prefs.Developer.StartupWebInspectorPane.Key)
				inspectorPanePopupButton.SelectItem (
					(int)Prefs.Developer.StartupWebInspectorPane.GetValue ());
		}
	}
}