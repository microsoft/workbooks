// PackageManagerWindow.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Client.Windows.Views
{
	partial class PackageManagerWindow : MetroWindow, IPackageManagerView, INotifyPropertyChanged
	{
		const string TAG = nameof (PackageManagerWindow);

		readonly PackageManagerViewModel viewModel;
		readonly ProgressSubscriptions progressSubscriptions = new ProgressSubscriptions ();
		CancellationTokenSource cancellationTokenSource;
		PackageViewModel selectedResult;
		PackageSourceViewModel selectedSource;
		bool showProgress;
		bool allowPrereleaseVersions;
		bool installingPackage;

		public BindingList<PackageViewModel> SearchResults { get; set; } = new BindingList<PackageViewModel> ();

		public PackageViewModel SelectedResult
		{
			get { return selectedResult; }
			set {
				selectedResult = value;
				OnPropertyChanged (nameof (CanAddPackage));
				OnPropertyChanged (nameof (InstallingPackage));
				OnPropertyChanged ();
			}
		}

		public string PackageQuery { get; set; }

		public bool InstallingPackage => installingPackage;

		public bool CanAddPackage => !installingPackage && SelectedResult != null && !ShowProgress;

		public bool ShowProgress
		{
			get { return showProgress; }
			set {
				showProgress = value;
				OnPropertyChanged ();
				OnPropertyChanged (nameof (CanAddPackage));
				OnPropertyChanged (nameof (InstallingPackage));
			}
		}

		public bool AllowPrereleaseVersions
		{
			get { return allowPrereleaseVersions; }
			set {
				allowPrereleaseVersions = value;
				OnPropertyChanged ();
				OnSearchTextChanged (null, null);
			}
		}

		public PackageSourceViewModel [] PackageSources => viewModel.PackageSources;

		public PackageSourceViewModel SelectedPackageSource {
			get { return selectedSource; }
			set {
				if (selectedSource == value)
					return;
				selectedSource = value;
				OnPropertyChanged ();
				OnSearchTextChanged (null, null);
			}
		}

		public PackageManagerWindow (ClientSession session)
		{
			viewModel = new PackageManagerViewModel (session, this);
			selectedSource = viewModel.PackageSources.FirstOrDefault ();

			InitializeComponent ();
			DataContext = this;

			progressSubscriptions.Changed += (o, e) => ShowProgress = progressSubscriptions.ShowProgress;
		}

		public void ClearPackages ()
			=> SearchResults.Clear ();

		public void AddPackageResult (PackageViewModel package)
			=> SearchResults.Add (package);

		async void OnSearchTextChanged (object sender, TextChangedEventArgs args)
		{
			Cancel ();

			using (var progressSubscription = progressSubscriptions.Subscribe ()) {
				var localCts = cancellationTokenSource = new CancellationTokenSource ();
				try {
					await viewModel.SearchForPackagesAsync (
						PackageQuery,
						AllowPrereleaseVersions,
						SelectedPackageSource,
						cancellationTokenSource.Token);
				} catch (TaskCanceledException) {
				} catch (OperationCanceledException) {
				} catch (Exception e) {
					if (localCts.IsCancellationRequested)
						return;

					progressSubscription.Dispose ();

					Log.Error (TAG, e);

					new MetroDialogWindow {
						Owner = this,
						Title = "Error searching NuGet",
						Width = Width,
						Message = e.Message,
						ButtonStyle = MessageDialogStyle.Affirmative,
					}.ShowDialog ();
				} finally {
					localCts.Cancel ();
				}
			}
		}

		async void OnAddSelectedPackage (object sender, RoutedEventArgs args)
		{
			var close = false;
			var package = SelectedResult;
			if (package == null || installingPackage)
				return;

			using (var progressSubscription = progressSubscriptions.Subscribe ()) {
				installingPackage = true;

				var localCts = cancellationTokenSource = new CancellationTokenSource ();
				try {
					await viewModel.AddPackageAsync (package, cancellationTokenSource.Token);
					close = true;
				} catch (TaskCanceledException) {
				} catch (Exception e) {
					if (localCts.IsCancellationRequested)
						return;

					progressSubscription.Dispose ();

					Log.Error (TAG, e);

					new MetroDialogWindow {
						Owner = this,
						Title = "Unable to add NuGet package to workbook",
						Width = Width,
						Message = e.Message,
						ButtonStyle = MessageDialogStyle.Affirmative,
					}.ShowDialog ();
				} finally {
					localCts.Cancel ();
				}

				installingPackage = false;
			}

			if (close)
				Close ();
		}

		public void Cancel ()
		{
			try {
				cancellationTokenSource?.Cancel ();
				cancellationTokenSource = null;
			} catch (ObjectDisposedException) {
			}
		}

		protected override void OnClosed (EventArgs e)
		{
			Cancel ();

			base.OnClosed (e);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged ([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
		}
	}
}
