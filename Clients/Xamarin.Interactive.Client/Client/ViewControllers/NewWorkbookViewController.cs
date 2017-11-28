//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Client.ViewControllers
{
    sealed class NewWorkbookViewController : INotifyPropertyChanged
    {
        const string TAG = nameof (NewWorkbookViewController);

        public ReadOnlyCollection<NewWorkbookItem> Items { get; }

        public AgentType SelectedAgentType {
            get => SelectedItem?.SelectedWorkbookApp?.AgentType ?? AgentType.Unknown;
            set => SetSelectedItem (a => a.AgentType == value);
        }

        NewWorkbookItem selectedItem;
        public NewWorkbookItem SelectedItem {
            get => selectedItem;
            set {
                if (selectedItem != value) {
                    selectedItem = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NewWorkbookViewController ()
        {
            Items = new ReadOnlyCollection<NewWorkbookItem> ((
                from app in WorkbookAppInstallation.All
                group app by app.Flavor into appGroup
                select new NewWorkbookItem (appGroup.First ().Icon, appGroup.Key, appGroup)
            ).ToList ());

            var lastUsedWorkbookApp = Prefs.UI.LastUsedWorkbookApp.GetValue ();
            if (lastUsedWorkbookApp == null && Items.Count > 0)
                selectedItem = Items [0];
            else
                LoadLastCreatedWorkbookPreference (lastUsedWorkbookApp);
        }

        public void SaveLastCreatedWorkbookPreference ()
        {
            try {
                var lastUsedWorkbookApp = JToken.FromObject (new {
                    id = SelectedItem.SelectedWorkbookApp.Id,
                    optionalFeatures = SelectedItem.SelectedWorkbookApp.OptionalFeatures
                        .Where (feature => feature.Enabled)
                        .Select (feature => feature.Id)
                }).ToString ();
                Prefs.UI.LastUsedWorkbookApp.SetValue (lastUsedWorkbookApp);
            } catch (Exception e) {
                Log.Warning (TAG, "Could not save last used workbook app preference.", e);
            }
        }

        void LoadLastCreatedWorkbookPreference (string lastUsedWorkbookAppJson)
        {
            JObject lastUsedWorkbookApp;

            try {
                lastUsedWorkbookApp = JObject.Parse (lastUsedWorkbookAppJson);

                var id = lastUsedWorkbookApp.Value<string> ("id");
                var optionalFeatures = new HashSet<string> (
                    lastUsedWorkbookApp.Value<JArray> ("optionalFeatures").Values<string> ());

                SetSelectedItem (wap => wap.Id == id);
                selectedItem.SelectedWorkbookApp.OptionalFeatures.ForEach (feature => {
                    if (optionalFeatures.Contains (feature.Id))
                        feature.Enabled = true;
                });
            } catch (Exception e) {
                Log.Warning (TAG, "Could not load last used workbook app from preferences.", e);
                selectedItem = Items [0];
                return;
            }
        }

        void SetSelectedItem (Func<WorkbookAppViewController, bool> workbookAppFilter)
        {
            foreach (var item in Items) {
                var app = item.WorkbookApps.FirstOrDefault (workbookAppFilter);
                if (app != null) {
                    SelectedItem = item;
                    item.SelectedWorkbookApp = app;
                    return;
                }
            }
        }

        void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
    }
}