//
// WorkbookAppViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Interactive.Workbook.NewWorkbookFeatures;

namespace Xamarin.Interactive.Client.ViewControllers
{
    sealed class WorkbookAppViewController
    {
        public enum Context
        {
            NewWorkbookDialog,
            Menu,
            ComboBox,
            StatusBar
        }

        public static WorkbookAppViewController SeparatorItem { get; } = new WorkbookAppViewController ();

        public bool IsSeparator => this == SeparatorItem;

        WorkbookAppViewController ()
        {
        }

        public AgentType AgentType { get; }
        public string Icon { get; }
        public bool Enabled { get; }
        public string Label { get; }
        public IReadOnlyList<NewWorkbookFeature> OptionalFeatures { get; }

        public WorkbookAppViewController (
            WorkbookAppInstallation workbookApp,
            Context context,
            bool enabled)
        {
            if (workbookApp == null)
                throw new ArgumentNullException (nameof (workbookApp));

            AgentType = workbookApp.GetAgentType ();

            Icon = workbookApp.Icon ?? "project";
            Enabled = enabled;
            Label = GetDisplayLabel (workbookApp, context);

            OptionalFeatures = workbookApp
                .OptionalFeatures
                .Where (id => NewWorkbookFeature.AllFeatures.ContainsKey (id))
                .Select (id => NewWorkbookFeature.AllFeatures [id])
                .ToArray ();
        }

        public static string GetDisplayLabel (WorkbookAppInstallation workbookApp, Context context)
        {
            switch (context) {
            case Context.NewWorkbookDialog:
                return string.IsNullOrEmpty (workbookApp.Sdk.Profile)
                    ? workbookApp.Sdk.Name
                    : $"{workbookApp.Sdk.Name} ({workbookApp.Sdk.Profile})";
            case Context.Menu:
            case Context.ComboBox:
                var name = workbookApp.Flavor;

                if (name == "Console" && !string.IsNullOrEmpty (workbookApp.Sdk.Name))
                    name += " (" + workbookApp.Sdk.Name + ")";

                if (!string.IsNullOrEmpty (workbookApp.Sdk.Profile))
                    name += " (" + workbookApp.Sdk.Profile + ")";

                return name;
            case Context.StatusBar:
                return string.IsNullOrEmpty (workbookApp.Sdk.Profile)
                    ? workbookApp.Sdk.Name
                    : $"{workbookApp.Sdk.Name} ({workbookApp.Sdk.Profile})";
            default:
                throw new NotImplementedException ($"{nameof (Context)}.{context}");
            }
        }
    }
}