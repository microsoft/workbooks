//
// UpdaterService.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Versioning;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Client.Updater
{
	sealed class UpdaterService
	{
		const string TAG = nameof (UpdaterService);

		readonly string operatingSystem;
		readonly string productId;
		readonly string defaultChannel;

		IDisposable preferenceChangeSubscripton;
		Timer checkTimer;
		int checkTimerGeneration;
		Action<Task<UpdateItem>> periodicUpdateHandler;
		int updateChecksPerformed;

		public string UpdateChannel
			=> Prefs.Updater.Channel.GetValue () ?? defaultChannel;

		public string [] AvailableChannels { get; } = new [] {
			"Stable",
			"Beta",
			"Alpha"
		};

		public UpdaterService (string operatingSystem, string productId, string defaultChannel = null)
		{
			this.operatingSystem = operatingSystem
				?? throw new ArgumentNullException (nameof (operatingSystem));

			this.productId = productId
				?? throw new ArgumentNullException (nameof (productId));

			if (defaultChannel == null)
				this.defaultChannel = BuildInfo.Version.CandidateLevel == ReleaseCandidateLevel.Stable
					? "Stable"
					: "Alpha";
			else
				this.defaultChannel = defaultChannel;
		}

		internal HttpClient CreateHttpClient ()
		{
			var httpClient = new HttpClient ();

			httpClient.DefaultRequestHeaders.UserAgent.Add (
				new ProductInfoHeaderValue ("XamarinInteractiveUpdater", BuildInfo.VersionString));

			return httpClient;
		}

		public void CheckForUpdatesPeriodicallyInBackground (Action<Task<UpdateItem>> updateHandler)
		{
			periodicUpdateHandler = updateHandler
				?? throw new ArgumentNullException (nameof (updateHandler));

			if (preferenceChangeSubscripton == null)
				preferenceChangeSubscripton = PreferenceStore.Default.Subscribe (ObservePreferenceChange);

			ScheduleCheckForUpdatesPeriodicallyInBackground ();
		}

		void ScheduleCheckForUpdatesPeriodicallyInBackground ()
		{
			MainThread.Ensure ();

			// timer callbacks may still be invoked after the timer is
			// disposed, so track a generation to know if we should
			// bail early if that circumstance arises.
			checkTimerGeneration++;
			checkTimer?.Dispose ();

			TimeSpan frequency;

			switch (Prefs.Updater.QueryFrequency.GetValue ()) {
			case QueryFrequency.Never:
				Log.Debug (TAG, "Skipping check (automatic update checking disabled)");
				return;
			case QueryFrequency.Hourly:
				frequency = TimeSpan.FromHours (1);
				break;
			case QueryFrequency.Daily:
				frequency = TimeSpan.FromDays (1);
				break;
			case QueryFrequency.Weekly:
				frequency = TimeSpan.FromDays (7);
				break;
			case QueryFrequency.Startup:
			default:
				if (updateChecksPerformed == 0) {
					Log.Debug (TAG, "Performing single automatic startup update check");
					CheckForUpdatesInBackground (false, periodicUpdateHandler);
				}

				Log.Debug (TAG, "Skipping check (already performed single automatic startup check)");
				return;
			}

			var lastCheck = Prefs.Updater.LastQuery.GetValue ();
			var nextCheck = frequency - (DateTime.UtcNow - lastCheck);
			if (nextCheck < TimeSpan.Zero)
				nextCheck = TimeSpan.Zero;

			Log.Debug (TAG, $"Scheduling check. Last check @ {lastCheck}; next in {nextCheck}");

			checkTimer = new Timer (state => MainThread.Post (() => {
				Log.Debug (TAG, "Update check timer expired");

				if ((int)state != checkTimerGeneration) {
					Log.Debug (TAG, "Ignoring update check timer callback from previous generation");
					return;
				}

				switch (Prefs.Updater.QueryFrequency.GetValue ()) {
				case QueryFrequency.Never:
					checkTimer?.Dispose ();
					return;
				case QueryFrequency.Startup:
					if (updateChecksPerformed > 0) {
						checkTimer?.Dispose ();
						return;
					}
					break;
				}

				CheckForUpdatesInBackground (false, periodicUpdateHandler);
			}), checkTimerGeneration, nextCheck, frequency);
		}

		void ObservePreferenceChange (PreferenceChange change)
		{
			if (change.Key == Prefs.Updater.QueryFrequency.Key)
				ScheduleCheckForUpdatesPeriodicallyInBackground ();
		}

		Task<UpdateItem> CheckForUpdatesAsync (
			CancellationToken cancellationToken = default (CancellationToken))
		{
			var targetChannel = UpdateChannel;

			return Task.Run (async () => {
				var uri = new StringBuilder ("https://software.xamarin.com/Service/Updates?v=2&pv")
					.Append (productId)
						.Append ('=')
						.Append (BuildInfo.UpdateVersion)
					.Append ("&m=").Append (productId)
					.Append ("&level=").Append (targetChannel)
					.Append ("&alevel=").Append (targetChannel)
					.Append ("&os=").Append (operatingSystem);

				Log.Debug (TAG, $"querying update service: {uri}");

				var httpClient = CreateHttpClient ();
				var response = (await httpClient.GetAsync (
					uri.ToString (),
					HttpCompletionOption.ResponseContentRead,
					cancellationToken)).EnsureSuccessStatusCode ();

				// NOTE: the await expression to produce 'manifest' is pulled out
				// of the null-coalescing expression that follows due to a bug in
				// mcs that results in an NRE if one object in the query is null
				// when the await is a subexpression of the null-coalescing chain
				// cf. https://bugzilla.xamarin.com/show_bug.cgi?id=52578
				var manifest = UpdateManifest
					.Deserialize (await response.Content.ReadAsStreamAsync ());

				var update = manifest
					?.Applications
					?.FirstOrDefault ()
					?.Updates
					?.FirstOrDefault ();

				if (update != null && !update.IsValid)
					throw new Exception (Catalog.GetString (
						"Update service returned an update but it could not be parsed."));

				updateChecksPerformed++;

				MainThread.Post (() => Prefs.Updater.LastQuery.SetValue (DateTime.UtcNow));

				if (update == null)
					return null;

				Log.Debug (TAG, $"update service returned update: {update.Version} published {update.Date}");

				var releaseNotesUrl = $"https://dl.xamarin.com/interactive/updater-release-notes-{update.Version}.html";

				try {
					httpClient = CreateHttpClient ();
					response = (await httpClient.GetAsync (
						releaseNotesUrl,
						HttpCompletionOption.ResponseContentRead,
						cancellationToken)).EnsureSuccessStatusCode ();
					update.ReleaseNotes = await response.Content.ReadAsStringAsync ();
				} catch (Exception e) when (!(e is TaskCanceledException || e is OperationCanceledException)) {
					Log.Warning (TAG, $"{update.Version} update available but release notes are missing: {releaseNotesUrl}", e);
				}

				ReleaseVersion.TryParse (update.Version, out var semver);

				return new UpdateItem (
					semver,
					update.Version,
					update.ReleaseNotes,
					targetChannel,
					update.Url,
					update.Hash);
			});
		}

		public CancellationTokenSource CheckForUpdatesInBackground (
			bool userInitiated,
			Action<Task<UpdateItem>> updateHandler)
		{
			if (updateHandler == null)
				throw new ArgumentNullException (nameof (updateHandler));

			MainThread.Ensure ();

			if (!userInitiated) {
				var skip = false;

				if (BuildInfo.IsLocalDebugBuild) {
					skip = true;
					Log.Info (TAG, "skipping update check since BuildInfo.IsLocalDebugBuild == true");
				} else if (CommandLineTool.TestDriver.ShouldRun) {
					skip = true;
					Log.Info (TAG, "skipping automatic update check under the tests driver");
				}

				if (skip) {
					updateHandler (Task.FromResult<UpdateItem> (null));
					return null;
				}
			}

			Log.Info (TAG, "checking for updates");

			var cancellationTokenSource = new CancellationTokenSource ();

			CheckForUpdatesAsync (cancellationTokenSource.Token).ContinueWith (task => {
				if (task.IsCanceled) {
					Log.Error (TAG, "update check cancelled");
					Telemetry.Events.UpdateEvent.Canceled ().Post ();
				} else if (task.IsFaulted) {
					Log.Error (TAG, "update check failed", task.Exception);
					Telemetry.Events.UpdateEvent.CheckFailed ().Post ();
				} else if (task.Result == null) {
					Log.Info (TAG, "no updates are available");
				} else {
					Log.Info (TAG, $"update available: {task.Result.DownloadUrl}");
					Telemetry.Events.UpdateEvent.Available (task.Result).Post ();
				}

				MainThread.Post (() => {
					try {
						updateHandler (task);
					} catch (Exception e) {
						Log.Error (TAG, "failed to run update handler", e);
					}
				});
			});

			return cancellationTokenSource;
		}
	}
}