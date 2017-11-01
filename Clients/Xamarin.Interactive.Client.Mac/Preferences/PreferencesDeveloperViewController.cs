//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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