//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Xamarin.Interactive.Client.ViewControllers
{
    sealed class NewWorkbookItem
    {
        public IReadOnlyList<WorkbookAppViewController> WorkbookApps { get; }
        public string IconName { get; }
        public string Label { get; }

        WorkbookAppViewController selectedWorkbookApp;
        public WorkbookAppViewController SelectedWorkbookApp {
            get => selectedWorkbookApp;
            set {
                if (selectedWorkbookApp != value) {
                    selectedWorkbookApp = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NewWorkbookItem (
            string iconName,
            string label,
            IEnumerable<WorkbookAppInstallation> workbookApps)
        {
            IconName = iconName;
            Label = label ?? throw new ArgumentNullException (nameof (label));

            if (workbookApps == null)
                throw new ArgumentNullException (nameof (workbookApps));

            if (!workbookApps.Any ())
                throw new ArgumentException (
                    "must have at least one workbook app",
                    nameof (workbookApps));

            WorkbookApps = workbookApps
                .Select (app => new WorkbookAppViewController (
                    app,
                    WorkbookAppViewController.Context.NewWorkbookDialog,
                    true))
                .ToArray ();

            selectedWorkbookApp = WorkbookApps [0];
        }

        void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        public ClientSessionUri CreateClientSessionUri ()
            => new ClientSessionUri (
                SelectedWorkbookApp.AgentType,
                ClientSessionKind.Workbook)
                .WithParameters (
                    SelectedWorkbookApp
                        .OptionalFeatures
                        .Where (f => f.Enabled)
                        .Select (f => new KeyValuePair<string, string> ("feature", f.Id)));
    }
}