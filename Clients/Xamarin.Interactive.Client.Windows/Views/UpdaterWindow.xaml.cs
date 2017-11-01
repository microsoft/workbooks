//
// UpdaterWindow.xaml.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Client.Windows.Views
{
	sealed partial class UpdaterWindow : MetroWindow
	{
		readonly IDisposable preferenceChangeSubscription;

		public readonly UpdaterViewModel updaterViewModel;

		public UpdaterWindow (UpdateItem updateItem)
		{
			if (updateItem == null)
				throw new ArgumentNullException (nameof (updateItem));

			updaterViewModel = new WpfUpdaterViewModel (this, updateItem);
			DataContext = updaterViewModel;

			InitializeComponent ();

			new XcbWebView (webBrowser);
			webBrowser.Loaded += (o, e) => webBrowser.NavigateToString (updaterViewModel.ReleaseNotes);
			webBrowser.Navigating += (o, e) => {
				if (e.Uri != null) {
					Process.Start (e.Uri.ToString ());
					e.Cancel = true;
				}
			};

			remindMeLaterButton.Click += (o, e) => Close ();
			downloadButton.Click += (o, e) => updaterViewModel.StartDownloadAsync ().Forget ();
			cancelButton.Click += (o, e) => updaterViewModel.CancelDownload ();

			preferenceChangeSubscription = PreferenceStore.Default.Subscribe (ObservePreferenceChange);
		}

		public bool IsWorking => updaterViewModel.IsProgressBarVisible;

		public Versioning.ReleaseVersion UpdateReleaseVersion => updaterViewModel.UpdateItem.ReleaseVersion;

		void ObservePreferenceChange (PreferenceChange obj)
		{
			if (obj.Key == Prefs.Updater.Channel.Key) {
				Close ();
				App.CheckForUpdatesInBackground (userInitiated: true);
			}
		}

		protected override void OnClosing (CancelEventArgs e)
		{
			if (IsWorking) {
				e.Cancel = true;
				return;
			}

			base.OnClosing (e);
		}

		protected override void OnClosed (EventArgs e)
		{
			base.OnClosed (e);

			preferenceChangeSubscription.Dispose ();

			App.CheckNeedsExit ();
		}

		void Hyperlink_Click (object sender, RoutedEventArgs e)
			=> Commands.Commands.ShowOptions.Execute (OptionsWindow.Tab.Updater);

		sealed class WpfUpdaterViewModel : UpdaterViewModel
		{
			const string TAG = nameof (WpfUpdaterViewModel);

			readonly Window ownerWindow;

			public WpfUpdaterViewModel (Window ownerWindow, UpdateItem updateItem)
				: base (ClientInfo.FullProductName, updateItem)
			{
				if (ownerWindow == null)
					throw new ArgumentNullException (nameof (ownerWindow));

				this.ownerWindow = ownerWindow;
			}

			MetroDialogWindow CreateDialog (string title, string message)
				=> new MetroDialogWindow {
					Owner = ownerWindow,
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
					Width = ownerWindow.Width,
					Title = title,
					Message = message
				};

			void DeleteUpdate ()
			{
				try {
					File.Delete (DownloadItem.TargetFile);
				} catch (Exception ex) {
					Log.Error (TAG, $"unable to delete file {DownloadItem.TargetFile}", ex);
				}
			}

			protected override Task InstallUpdateAsync ()
			{
				MainThread.Ensure ();

				var window = CreateDialog (
					Catalog.Format (Catalog.GetString (
						"{0} must be closed before continuing.",
						comment: "{0} is the application name"),
						AppName),
					Catalog.Format (Catalog.GetString (
						"The update was downloaded successfully and is ready " +
						"to be installed, but {0} must be closed first.",
						comment: "{0} is the application name"),
						AppName));

				window.ButtonStyle = MessageDialogStyle.AffirmativeAndNegative;
				window.AffirmativeButtonText = Catalog.GetString ("Quit & Install Update");
				window.NegativeButtonText = Catalog.GetString ("Cancel Update");

				window.ShowDialog ();

				if (window.Result != MessageDialogResult.Affirmative) {
					DeleteUpdate ();
					ownerWindow.Close ();
					return Task.CompletedTask;
				}

				Process.Start (DownloadItem.TargetFile).WaitForInputIdle (2000);
				App.Current.Shutdown ();

				return Task.CompletedTask;
			}

			protected override void RunErrorDialog (bool isDownloadError, string message)
			{
				MainThread.Ensure ();

				MetroDialogWindow dialog;

				if (isDownloadError) {
					dialog = CreateDialog (Catalog.GetString ("Download Failed"), message);
					dialog.ButtonStyle = MessageDialogStyle.AffirmativeAndNegative;
					dialog.AffirmativeButtonText = Catalog.GetString ("Download & Install Manually");
					dialog.NegativeButtonText = Catalog.GetString ("Cancel");
				} else {
					dialog = CreateDialog (Catalog.GetString ("Installation Failed"), message);
					dialog.ButtonStyle = MessageDialogStyle.Affirmative;
					dialog.AffirmativeButtonText = Catalog.GetString ("Close");
				}

				dialog.ShowDialog ();

				if (dialog.Result == MessageDialogResult.Affirmative && isDownloadError) {
					try {
						Process.Start (UpdateItem.DownloadUrl.ToString ());
					} catch (Exception e) {
						Log.Error (TAG, $"unable to Process.Start({UpdateItem.DownloadUrl})", e);
					}
				}
			}
		}
	}
}