//
// NewWorkbookViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using Xamarin.Interactive.Client.AgentProcesses;

namespace Xamarin.Interactive.Client.ViewControllers
{
	sealed class NewWorkbookViewController : INotifyPropertyChanged
	{
		public ReadOnlyCollection<NewWorkbookItem> Items { get; }

		public AgentType SelectedAgentType {
			get => SelectedItem?.SelectedWorkbookApp?.AgentType ?? AgentType.Unknown;
			set {
				foreach (var item in Items) {
					var app = item.WorkbookApps.FirstOrDefault (a => a.AgentType == value);
					if (app != null) {
						SelectedItem = item;
						item.SelectedWorkbookApp = app;
						return;
					}
				}
			}
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

			if (Items.Count > 0)
				SelectedItem = Items [0];
		}

		void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
	}
}