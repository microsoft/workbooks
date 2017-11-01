//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Client.Mac;
using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Preferences
{
    sealed partial class PreferencesUpdaterViewController : PreferencesViewController
    {
        PreferencesUpdaterViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            var visualStudio = VisualStudioForMacComponent.Installation;

            var name = visualStudio?.Name ?? "Visual Studio";

            switchChannelsButton.Title = Catalog.Format (Catalog.GetString (
                "Switch Update Channels in {0}",
                comment: "{0} is an app name ('Visual Studio')"),
                name);

            switchChannelsButton.Activated += (sender, args) => {
                try {
                    visualStudio.Launch ();
                } catch (Exception e) {
                    e.ToUserPresentable (Catalog.Format (Catalog.GetString (
                        "Unable to launch {0}, which must be " +
                        "installed to switch update channels.",
                        comment: "{0} is an app name ('Xamarin Studio')"),
                        name
                    )).Present ();
                }
            };

            foreach (var frequency in QueryFrequencyNames.All)
                frequencyPopUpButton.AddItem (frequency);

            frequencyPopUpButton.Activated += (sender, e) =>
                Prefs.Updater.QueryFrequency.SetValue (
                    (QueryFrequency)(int)frequencyPopUpButton.IndexOfSelectedItem);

            channelTextField.StringValue = ClientApp.SharedInstance.Updater.UpdateChannel;

            ObservePreferenceChange (new PreferenceChange (Prefs.Updater.QueryFrequency.Key));
            ObservePreferenceChange (new PreferenceChange (Prefs.Updater.LastQuery.Key));
        }

        protected override void ObservePreferenceChange (PreferenceChange change)
        {
            if (change.Key == Prefs.Updater.QueryFrequency.Key)
                frequencyPopUpButton.SelectItem ((int)Prefs.Updater.QueryFrequency.GetValue ());
            else if (change.Key == Prefs.Updater.LastQuery.Key) {
                var time = Prefs.Updater.LastQuery.GetValue ();
                if (time == DateTime.MinValue)
                    lastCheckedTextField.StringValue = Catalog.GetString ("Never");
                else
                    lastCheckedTextField.StringValue = time.ToLocalTime ().ToString ("g");
            }
        }
    }
}