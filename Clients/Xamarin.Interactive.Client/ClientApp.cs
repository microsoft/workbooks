//
// ClientApp.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Markdown;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive
{
	abstract class ClientApp
	{
		public struct ClientAppPaths
		{
			public FilePath LogFileDirectory { get; }
			public FilePath PreferencesDirectory { get; }
			public FilePath CacheDirectory { get; }

			public ClientAppPaths (
				FilePath logFileDirectory,
				FilePath preferencesDirectory,
				FilePath cacheDirectory)
			{
				LogFileDirectory = logFileDirectory;
				PreferencesDirectory = preferencesDirectory;
				CacheDirectory = cacheDirectory;
			}
		}

		const string TAG = nameof (ClientApp);

		public static ClientApp SharedInstance { get; private set; }

		FileStream logStream;

		public ClientAppPaths Paths { get; private set; }
		public Telemetry.Client Telemetry { get; private set; }
		public IPreferenceStore Preferences { get; private set; }
		public HostEnvironment Host { get; private set; }
		public IFileSystem FileSystem { get; private set; }
		public ClientWebServer WebServer { get; private set; }
		public UpdaterService Updater { get; private set; }

		protected abstract ClientAppPaths CreateClientAppPaths ();
		protected abstract IPreferenceStore CreatePreferenceStore ();
		protected abstract HostEnvironment CreateHostEnvironment ();
		protected abstract IFileSystem CreateFileSystem ();
		protected abstract ClientWebServer CreateClientWebServer ();
		protected abstract UpdaterService CreateUpdaterService ();

		sealed class InitializeException<T> : Exception
		{
			public InitializeException (string createMethod)
				: base ($"{createMethod} must return a valid {typeof (T)} instance")
			{
			}
		}

		protected ClientApp ()
			=> MainThread.Initialize ();

		public void Initialize (bool asSharedInstance = true)
		{
			Log.Initialize (new LogProvider (
				#if DEBUG
				LogLevel.Debug
				#else
				LogLevel.Info
				#endif
			));

			Paths = CreateClientAppPaths ();

			ConfigureLogging ();

			var nl = Environment.NewLine;
			Log.Commit (
				LogLevel.Info,
				LogFlags.NoFlair,
				null,
				$"{nl}{nl}{ClientInfo.FullProductName}{nl}" + 
				$"{BuildInfo.Copyright.Replace ("\n", nl)}{nl}" +
				$"├─ Version: {BuildInfo.Version}{nl}" +
				$"├─ Date: {BuildInfo.Date}{nl}" +
				$"├─ Hash: {BuildInfo.Hash}{nl}" +
				$"├─ Branch: {BuildInfo.Branch}{nl}" +
				$"└─ Lane: {BuildInfo.BuildHostLane}{nl}");

			Preferences = CreatePreferenceStore ()
				?? throw new InitializeException<IPreferenceStore> (
					nameof (CreatePreferenceStore));

			PreferenceStore.Default = Preferences;

			Telemetry = new Telemetry.Client ();

			Host = CreateHostEnvironment ()
				?? throw new InitializeException<HostEnvironment> (
					nameof (CreateHostEnvironment));

			FileSystem = CreateFileSystem ()
				?? throw new InitializeException<IFileSystem> (
					nameof (CreateFileSystem));

			Log.SetLogLevel (Prefs.Logging.Level.GetValue ());

			WebServer = CreateClientWebServer ()
				?? throw new InitializeException<ClientWebServer> (
					nameof (CreateClientWebServer));

			Updater = CreateUpdaterService ()
				?? throw new InitializeException<UpdaterService> (
					nameof (CreateUpdaterService));

			if (asSharedInstance)
				SharedInstance = this;

			InteractiveInstallation.InitializeDefault (
				Host.IsMac,
				DevEnvironment.RepositoryRootDirectory);

			new Telemetry.Events.AppSessionStart ().Post ();

			DeleteLegacyPackageCacheInBackground ();
		}

		void ConfigureLogging ()
		{
			try {
				File.Delete (Paths.LogFileDirectory);
			} catch {
			}

			try {
				Directory.CreateDirectory (Paths.LogFileDirectory);

				// delete all except a handful of the most recent log files
				foreach (var file in Directory.EnumerateFiles (Paths.LogFileDirectory, "*.log")
					.OrderByDescending (path => File.GetLastWriteTimeUtc (path))
					.Skip (4)) {
					try {
						File.Delete (file);
					} catch {
					}
				}
			} catch {
			}

			try {
				logStream = File.Open (Paths.LogFileDirectory.Combine (
					$"Xamarin Inspector {DateTime.Now:yyyy-MM-dd}.log"),
					FileMode.Append,
					FileAccess.Write,
					FileShare.Read);

				Log.EntryAdded += LogEntryAdded;

				AppDomain.CurrentDomain.UnhandledException += (s, e) =>
					Log.Critical (
						TAG + ":AppDomain.UnhandledException",
						(Exception)e.ExceptionObject);
			} catch (Exception e) {
				Log.Error (TAG, e);
			}
		}

		void LogEntryAdded (object provider, LogEntry entry)
		{
			var bytes = Utf8.GetBytes (entry + Environment.NewLine);
			logStream.Write (bytes, 0, bytes.Length);
			logStream.Flush ();

			if (entry.Flags.HasFlag (LogFlags.SkipTelemetry))
				return;

			switch (entry.Level) {
			case LogLevel.Critical:
			case LogLevel.Error:
				new Telemetry.Events.Error (entry).Post ();
				break;
			}
		}

		/// <summary>
		/// Deletes the legacy package cache if it exists, on a background thread.
		/// </summary>
		/// <remarks>
		/// Introducedin 1.3 when migrating from NuGet 2.0 (where we had our own private package)
		/// cache to 4.0 where we use the shared package cache like the IDEs. We can remove this
		/// in the future (say, in late 2018?) once there are no significant numbers of 1.2.x
		/// or older clients in the wild. -abock, 2017-10-25
		/// </remarks>
		void DeleteLegacyPackageCacheInBackground ()
		{
			var packagesCacheDir = Paths.CacheDirectory.Combine ("packages");
			if (!packagesCacheDir.DirectoryExists)
				return;

			new Thread (() => {
				var sw = new Stopwatch ();
				sw.Start ();

				Log.Info (TAG, $"Deleting legacy package cache: {packagesCacheDir}");

				Exception ex = null;
				try {
					Directory.Delete (packagesCacheDir, true);
				} catch (Exception e) {
					// User may have a folder open or something. Just try again next time.
					ex = e;
				} finally {
					sw.Stop ();
				}

				if (ex == null)
					Log.Info (TAG, $"Deleted legacy package cache in {sw.ElapsedMilliseconds}ms");
				else
					Log.Error (
						TAG,
						$"Error deleting legacy package cache " +
						"(after {sw.ElapsedMilliseconds}ms)",
						ex);
			}) {
				IsBackground = true,
			}.Start ();
		}


		public string GetIssueReportForClipboard ()
		{
			var writer = new StringWriter ();
			WriteIssueReportForClipboardAsync (writer).GetAwaiter ().GetResult ();
			return writer.ToString ().TrimEnd ();
		}

		public async Task WriteIssueReportForClipboardAsync (TextWriter writer)
		{
			var osArch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

			new MarkdownTable (ClientInfo.FullProductName, "Detail", "Value") {
				{ "Version", BuildInfo.Version.ToString () },
				{ "Git Branch", BuildInfo.Branch },
				{ "Git Hash", BuildInfo.HashShort },
				{ "VSTS Definition", BuildInfo.BuildHostLane }
			}.Render (writer);

			writer.WriteLine ();

			string cores = null;

			if (Host.ActiveProcessorCount != null)
				cores = $"{Host.ActiveProcessorCount}";

			if (Host.ProcessorCount != null) {
				if (cores != null)
					cores += " / ";
				cores += $"{Host.ProcessorCount}";
			}

			cores = cores ?? "_Unknown_";

			string memory = null;

			if (Host.PhysicalMemory != null)
				memory = $"{Host.PhysicalMemory.Value / 1_073_741_824.0:N0} GB";

			memory = memory ?? "_Unknown_";

			new MarkdownTable ("System Info", "Component", "Value") {
				{ $"{Host.OSName}", $"{Host.OSVersionString} ({osArch})" },
				{ "CPU Cores", cores },
				{ "Physical Memory", memory }
			}.Render (writer);

			var softwareEnvironments = await Host.GetSoftwareEnvironmentsAsync ();
			if (softwareEnvironments == null)
				return;

			foreach (var environment in softwareEnvironments) {
				var name = environment is SystemSoftwareEnvironment
					? "System-Installed Software"
					: $"{environment.Name} Components";

				MarkdownTable table = null;

				foreach (var component in environment.Where (c => c.IsInstalled)) {
					if (table == null)
						table = new MarkdownTable (name, "Component", "Version");
					table.Add (component.Name, component.Version);
				}

				if (table != null) {
					writer.WriteLine ();
					table.Render (writer);
				}
			}
		}
	}
}