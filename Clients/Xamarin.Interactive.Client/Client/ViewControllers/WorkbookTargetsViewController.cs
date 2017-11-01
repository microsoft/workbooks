//
// WorkbookTargetsViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Xamarin.Interactive.Client.ViewControllers
{
	sealed class WorkbookTargetsViewController : ObservableCollection<WorkbookAppViewController>, INotifyPropertyChanged
	{
		public new event PropertyChangedEventHandler PropertyChanged;

		WorkbookAppViewController selectedTarget;
		public WorkbookAppViewController SelectedTarget {
			get { return selectedTarget; }
			set {
				if (selectedTarget != value) {
					selectedTarget = value;
					PropertyChanged?.Invoke (
						this,
						new PropertyChangedEventArgs (nameof (SelectedTarget)));
				}
			}
		}

		bool isWorkbookSession;

		public bool IsVisible => isWorkbookSession && SelectedTarget != null;

		public void UpdateTargets (ClientSession clientSession)
		{
			isWorkbookSession = clientSession != null && clientSession.SessionKind == ClientSessionKind.Workbook;

			SelectedTarget = null;
			Clear ();

			var preferredWorkbookApps = clientSession
				.Workbook
				.PlatformTargets
				.Select (WorkbookAppInstallation.Locate)
				.Where (app => app != null)
				.ToList ();

			var havePreferredWorkbookApp = false;

			foreach (var workbookApp in preferredWorkbookApps) {
				havePreferredWorkbookApp = true;
				var item = new WorkbookAppViewController (
					workbookApp,
					WorkbookAppViewController.Context.ComboBox,
					true);

				Add (item);

				if (item.Enabled && SelectedTarget == null)
					SelectedTarget = item;
			}

			var addedSeparator = false;

			foreach (var workbookApp in WorkbookAppInstallation.All) {
				if (!preferredWorkbookApps.Contains (workbookApp)) {
					if (havePreferredWorkbookApp && !addedSeparator) {
						addedSeparator = true;
						Add (WorkbookAppViewController.SeparatorItem);
					}

					Add (new WorkbookAppViewController (
						workbookApp,
						WorkbookAppViewController.Context.ComboBox,
						true));
				}
			}

			if (SelectedTarget == null && Count > 0)
				SelectedTarget = this [0];
		}
	}
}