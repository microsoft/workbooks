//
// UpdaterViewModel.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Versioning;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Updater
{
	abstract class UpdaterViewModel : INotifyPropertyChanged
	{
		const string TAG = nameof (UpdaterViewModel);

		public UpdateItem UpdateItem { get; }
		public DownloadItem DownloadItem { get; }
		public string AppName { get; }
		public string PromptMessage { get; }
		public string ReleaseNotes { get; }

		CancellationTokenSource cancellationTokenSource;

		public UpdaterViewModel (string appName, UpdateItem updateItem)
		{
			if (updateItem == null)
				throw new ArgumentNullException (nameof (updateItem));

			UpdateItem = updateItem;
			ReleaseNotes = updateItem.ReleaseNotes;

			AppName = appName;
			PromptMessage = Catalog.Format (Catalog.GetString (
				"{0} {1} is now available from the Xamarin {2} channel. " +
				"You are currently using version {3}. " +
				"Would you like to download and install the new version now?",
				comment: "{0} is the app name (Workbooks or Inspector), " +
					"{2} is a channel name (alpha, beta, stable), " +
					"{1} and {3} are version numbers"),
				AppName,
				updateItem.ReleaseVersion.IsValid
					? updateItem.ReleaseVersion.ToString (ReleaseVersionFormat.FriendlyShort)
					: updateItem.Version,
				updateItem.Channel,
				BuildInfo.Version.ToString (ReleaseVersionFormat.FriendlyShort));

			DownloadItem = new DownloadItem (
				UpdateItem.DownloadUrl,
				ClientApp.SharedInstance.FileSystem.GetTempDirectory ("updates"),
				UpdateItem.Md5Hash,
				useExactFileName: true);

			Reset ();
		}

		void Reset ()
		{
			IsProgressBarVisible = false;
			IsRemindMeLaterButtonVisible = true;
			IsDownloadButtonVisible = true;
			IsCancelButtonVisible = false;

			cancellationTokenSource = null;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

		public string DownloadButtonLabel { get; } = Catalog.GetString ("Download & Install");
		public string RemindMeLaterButtonLabel { get; } = Catalog.GetString ("Remind me Later");
		public string CancelButtonLabel { get; } = Catalog.GetString ("Cancel");

		bool isProgressBarVisible;
		public bool IsProgressBarVisible {
			get { return isProgressBarVisible; }
			set {
				if (isProgressBarVisible != value) {
					isProgressBarVisible = value;
					NotifyPropertyChanged ();
				}
			}
		}

		bool isRemindMeLaterButtonVisible;
		public bool IsRemindMeLaterButtonVisible {
			get { return isRemindMeLaterButtonVisible; }
			set {
				if (isRemindMeLaterButtonVisible != value) {
					isRemindMeLaterButtonVisible = value;
					NotifyPropertyChanged ();
				}
			}
		}

		bool isDownloadButtonVisible;
		public bool IsDownloadButtonVisible {
			get { return isDownloadButtonVisible; }
			set {
				if (isDownloadButtonVisible != value) {
					isDownloadButtonVisible = value;
					NotifyPropertyChanged ();
				}
			}
		}

		bool isCancelButtonVisible;
		public bool IsCancelButtonVisible {
			get { return isCancelButtonVisible; }
			set {
				if (isCancelButtonVisible != value) {
					isCancelButtonVisible = value;
					NotifyPropertyChanged ();
				}
			}
		}

		public void CancelDownload ()
			=> cancellationTokenSource?.Cancel ();

		public void RemindMeLater ()
			=> Telemetry.Events.UpdateEvent.Ignored (UpdateItem).Post ();

		public async Task StartDownloadAsync ()
		{
			CancelDownload ();

			cancellationTokenSource = new CancellationTokenSource ();

			IsProgressBarVisible = true;
			IsRemindMeLaterButtonVisible = false;
			IsDownloadButtonVisible = false;
			IsCancelButtonVisible = true;

			try {
				Telemetry.Events.UpdateEvent.Downloading (UpdateItem).Post ();
				await DownloadItem.DownloadAsync (cancellationTokenSource.Token);
			} catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException) {
				Telemetry.Events.UpdateEvent.Canceled (UpdateItem).Post ();
				return;
			} catch (Exception e) {
				var message = e.Message;
				if (e is DamagedDownloadException)
					message = Catalog.GetString ("The downloaded update failed to verify.");

				Log.Error (TAG, $"error downloading {DownloadItem.ActualSourceUri}", e);
				Reset ();
				MainThread.Post (() => RunErrorDialog (true, message));
				Telemetry.Events.UpdateEvent.Failed (UpdateItem).Post ();
				return;
			} finally {
				Log.Info (TAG, $"download operation lasted {DownloadItem.ElapsedTime}");
				Reset ();
			}

			Telemetry.Events.UpdateEvent.Installing (UpdateItem).Post ();

			try {
				await InstallUpdateAsync ();
			} catch (Exception e) {
				Log.Error (TAG, $"error installing {DownloadItem.TargetFile}", e);
				MainThread.Post (() => RunErrorDialog (false, e.Message));
			}
		}

		protected abstract Task InstallUpdateAsync ();

		protected abstract void RunErrorDialog (bool isDownloadError, string message);
	}
}
