//
// PackageManagerWindowController.cs
//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using AppKit;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Client.Mac
{
	sealed partial class PackageManagerWindowController : NSWindowController, IPackageManagerView
	{
		const string TAG = nameof(PackageManagerWindowController);

		readonly PackageManagerViewModel viewModel;
		CancellationTokenSource cancellationTokenSource;

		PackageListDataSource resultsDataSource;
		PackageListTableViewDelegate resultsTableViewDelegate;

		public PackageManagerWindowController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public PackageManagerWindowController (NSCoder coder) : base (coder)
		{
		}

		public PackageManagerWindowController (ClientSession clientSession)
			: base ("PackageManagerWindow")
		{
			viewModel = new PackageManagerViewModel (clientSession, this);
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			progressIndicator.StopAnimation (this);

			resultsDataSource = new PackageListDataSource ();
			resultsTableViewDelegate = new PackageListTableViewDelegate (this);
			searchResultsTableView.DataSource = resultsDataSource;
			searchResultsTableView.Delegate = resultsTableViewDelegate;

			foreach (var source in viewModel.PackageSources)
				packageSourcesPopUpButton.AddItem (source.DisplayName);

			searchField.Activated += async (s, e) => await UpdateSearchResults ();
			packageSourcesPopUpButton.Activated += async (s, e) => await UpdateSearchResults ();
			preReleaseCheckButton.Activated += async (s, e) => await UpdateSearchResults ();

			cancelButton.Activated += (s, e) => {
				CancelSearch ();
				Close ();
			};

			searchResultsTableView.DoubleClick += async (s, e) => await AddSelectedPackage ();
			addButton.Activated += async (s, e) => await AddSelectedPackage ();
		}

		async Task UpdateSearchResults ()
		{
			CancelSearch ();

			progressIndicator.StartAnimation (this);

			var localCts = cancellationTokenSource = new CancellationTokenSource ();
			try {
				var source =
					viewModel.PackageSources [packageSourcesPopUpButton.IndexOfSelectedItem];

				await viewModel.SearchForPackagesAsync (
					searchField.StringValue,
					preReleaseCheckButton.State == NSCellStateValue.On,
					source,
					cancellationTokenSource.Token);
			} catch (TaskCanceledException) {
			} catch (OperationCanceledException) {
			} catch (Exception ex) {
				Log.Error (TAG, ex);
				InvokeOnMainThread (() => {
					new NSAlert {
						MessageText = "Error searching NuGet",
						InformativeText = ex.Message,
						AlertStyle = NSAlertStyle.Critical
					}.BeginSheet (Window);
				});
			} finally {
				localCts.Cancel ();
			}

			progressIndicator.StopAnimation (this);
		}

		async Task AddSelectedPackage ()
		{
			var row = (int)searchResultsTableView.SelectedRow;
			if (row < 0)
				return;

			var package = resultsDataSource.Packages [row];
			var close = false;
			Exception addPackageEx = null;

			var localCts = cancellationTokenSource = new CancellationTokenSource ();
			using (var packageAddWindowController = new PackageAddWindowController (
				package,
				cancellationTokenSource)) {
				try {
					Window.BeginSheet (packageAddWindowController.Window, result => { });

					await viewModel.AddPackageAsync (
						package, cancellationTokenSource.Token);

					close = true;
				} catch (TaskCanceledException) {
				} catch (Exception ex) {
					Log.Error (TAG, ex);
					addPackageEx = ex;
				} finally {
					localCts.Cancel ();
					Window.EndSheet (packageAddWindowController.Window);
				}
			}

			if (addPackageEx != null)
				new NSAlert {
					MessageText = "Unable to add NuGet package to workbook",
					InformativeText = addPackageEx.Message,
					AlertStyle = NSAlertStyle.Critical
				}.BeginSheet (Window);

			if (close)
				Close ();
		}

		public void ClearPackages ()
		{
			resultsDataSource.Packages.Clear ();
			ReloadResults ();
		}

		public void AddPackageResult (PackageViewModel package)
		{
			resultsDataSource.Packages.Add (package);
			ReloadResults ();
		}

		void ReloadResults ()
		{
			var selectedRow = searchResultsTableView.SelectedRow;
			searchResultsTableView.ReloadData ();
			if (selectedRow >= 0 && selectedRow < searchResultsTableView.RowCount)
				searchResultsTableView.SelectRow (selectedRow, false);
		}

		void CancelSearch ()
		{
			try {
				cancellationTokenSource?.Cancel ();
				cancellationTokenSource = null;
			} catch (ObjectDisposedException) {
			}

			progressIndicator.StopAnimation (this);
		}

		public new PackageManagerWindow Window {
			get { return (PackageManagerWindow)base.Window; }
		}

		class PackageListDataSource : NSTableViewDataSource
		{
			public List<PackageViewModel> Packages { get; } = new List<PackageViewModel> ();

			public override nint GetRowCount (NSTableView tableView) => Packages.Count;
		}

		class PackageListTableViewDelegate : NSTableViewDelegate
		{
			const string ViewId = "PackageNameView";

			readonly PackageManagerWindowController windowController;

			public PackageListTableViewDelegate (PackageManagerWindowController windowController)
			{
				this.windowController = windowController;
			}

			public override void SelectionDidChange (NSNotification notification)
			{
				windowController.addButton.Enabled
					= windowController.searchResultsTableView.SelectedRowCount > 0;
			}

			public override NSView GetViewForItem (NSTableView tableView,
				NSTableColumn tableColumn, nint row)
			{
				if (row < 0 || row >= windowController.resultsDataSource.Packages.Count)
					return null;

				var view = (NSTableCellView)tableView.MakeView (ViewId, this);
				var package = windowController.resultsDataSource.Packages [(int)row];
				view.TextField.StringValue = package.DisplayName;
				return view;
			}
		}
	}
}
