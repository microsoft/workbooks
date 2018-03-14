//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive
{
    abstract class ClientApp
    {
        public struct ClientAppPaths
        {
            public FilePath LogFileDirectory { get; }
            public FilePath SessionLogFile { get; }
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

                SessionLogFile = LogFileDirectory.Combine (
                    $"{ClientInfo.FullProductName} {DateTime.Now:yyyy-MM-dd}.log");
            }
        }

        const string TAG = nameof (ClientApp);

        public static ClientApp SharedInstance { get; private set; }

        FileStream logStream;

        public Guid AppSessionId { get; } = Guid.NewGuid ();

        public ClientAppPaths Paths { get; private set; }
        public Telemetry.Client Telemetry { get; private set; }
        public IPreferenceStore Preferences { get; private set; }
        public ClientAppHostEnvironment Host { get; private set; }
        public IssueReport IssueReport { get; private set; }
        public IFileSystem FileSystem { get; private set; }
        public ClientWebServer WebServer { get; private set; }
        public UpdaterService Updater { get; private set; }

        protected abstract ClientAppPaths CreateClientAppPaths ();
        protected abstract IPreferenceStore CreatePreferenceStore ();
        protected abstract ClientAppHostEnvironment CreateHostEnvironment ();
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

        public void Initialize (
            bool asSharedInstance = true,
            ILogProvider logProvider = null)
        {
            Log.Initialize (logProvider ?? new LogProvider (
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

            Log.Info (TAG, $"AppSessionId: {AppSessionId}");

            Preferences = CreatePreferenceStore ()
                ?? throw new InitializeException<IPreferenceStore> (
                    nameof (CreatePreferenceStore));

            Preferences.Remove ("telemetry.userGuid");

            PreferenceStore.Default = Preferences;

            Telemetry = new Telemetry.Client ();

            Host = CreateHostEnvironment ()
                ?? throw new InitializeException<ClientAppHostEnvironment> (
                    nameof (CreateHostEnvironment));

            IssueReport = new IssueReport (Host);

            FileSystem = CreateFileSystem ()
                ?? throw new InitializeException<IFileSystem> (
                    nameof (CreateFileSystem));

            Log.SetLogLevel (Prefs.Logging.Level.GetValue ());

            WebServer = CreateClientWebServer ()
                ?? throw new InitializeException<ClientWebServer> (
                    nameof (CreateClientWebServer));

            Updater = CreateUpdaterService ();

            if (asSharedInstance)
                SharedInstance = this;

            // Support both Mac and Windows installation paths in the base class
            // so that ConsoleClientApp does not need to be built differently on
            // different platforms.
            InteractiveInstallation.InitializeDefault (
                Host.IsMac
                    ? GetMacInstallationPaths ()
                    : GetWindowsInstallationPaths ());

            PostAppSessionStarted ();

            DeleteLegacyPackageCacheInBackground ();

            OnInitialized ();
        }

        void PostAppSessionStarted ()
        {
            var appSession = new Telemetry.Models.AppSession {
                AppSessionId = AppSessionId,
                Timestamp = DateTimeOffset.UtcNow,
                Version = BuildInfo.VersionString,
                BuildHash = BuildInfo.Hash,
                UpdateChannel = Updater?.UpdateChannel,
                OperatingSystem = new Telemetry.Models.OperatingSystem {
                    Version = Host.OSVersion.ToString (),
                    WordSize = (byte)IntPtr.Size,
                    CpuWordSize = (byte)(Environment.Is64BitOperatingSystem ? 8 : 4)
                }
            };

            switch (Host.OSName) {
            case HostOS.macOS:
                appSession.OperatingSystem.Name = Interactive.Telemetry.Models.OperatingSystemName.macOS;
                break;
            case HostOS.Windows:
                appSession.OperatingSystem.Name = Interactive.Telemetry.Models.OperatingSystemName.Windows;
                break;
            }

            appSession.Post ();
        }

        protected virtual void OnInitialized ()
        {
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
                logStream = File.Open (
                    Paths.SessionLogFile,
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

        public static InteractiveInstallationPaths GetWindowsInstallationPaths ()
        {
            var frameworkInstallPath = new FilePath (Assembly.GetExecutingAssembly ().Location)
                .ParentDirectory
                .ParentDirectory;

            if (!frameworkInstallPath.DirectoryExists)
                return null;

            return new InteractiveInstallationPaths (
                agentsInstallPath: frameworkInstallPath,
                toolsInstallPath: Path.Combine (frameworkInstallPath, "Tools"));
        }

        public static InteractiveInstallationPaths GetMacInstallationPaths ()
        {
            var agentsInstallPath = new FilePath (Assembly.GetExecutingAssembly ().Location)
                .ParentDirectory
                .ParentDirectory
                .Combine ("SharedSupport");

            if (!agentsInstallPath.DirectoryExists)
                return null;

            return new InteractiveInstallationPaths (agentsInstallPath);
        }
    }
}