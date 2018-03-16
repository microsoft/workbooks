//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Foundation;

using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.SystemInformation;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client
{
    sealed class MacClientApp : ClientApp
    {
        const string TAG = nameof (MacClientApp);

        sealed class MacHostEnvironment : ClientAppHostEnvironment
        {
            public override HostOS OSName { get; } = HostOS.macOS;
            public override string OSVersionString { get; }
            public override Version OSVersion { get; }

            public override ulong? PhysicalMemory
                => NSProcessInfo.ProcessInfo.PhysicalMemory;

            public override int? ActiveProcessorCount
                => (int)NSProcessInfo.ProcessInfo.ActiveProcessorCount;

            public override int? ProcessorCount
                => (int)NSProcessInfo.ProcessInfo.ProcessorCount;

            public MacHostEnvironment () : base (LoadSoftwareEnvironmentsAsync)
            {
                var procInfo = NSProcessInfo.ProcessInfo;
                var osVersion = procInfo.OperatingSystemVersion;
                OSVersionString = procInfo.OperatingSystemVersionString;
                OSVersion = new Version (
                    (int)osVersion.Major,
                    (int)osVersion.Minor,
                    (int)osVersion.PatchVersion);
            }

            static Task<IReadOnlyList<ISoftwareEnvironment>> LoadSoftwareEnvironmentsAsync ()
                => Task.Run<IReadOnlyList<ISoftwareEnvironment>> (() => new [] {
                    new SystemSoftwareEnvironment {
                        new XamarinComponent ("Mono"),
                        new XamarinComponent ("Mac"),
                        new XamarinComponent ("iOS"),
                        new XamarinComponent ("Android"),
                        VisualStudioForMacComponent.Installation
                    }
                });
        }

        protected override ClientAppPaths CreateClientAppPaths ()
        {
            var userLibraryDirectory = new FilePath (NSSearchPath.GetDirectories (
                NSSearchPathDirectory.LibraryDirectory,
                NSSearchPathDomain.User,
                true).FirstOrDefault ());

            var flavor = ClientInfo.Flavor.ToString ();

            return new ClientAppPaths (
                userLibraryDirectory.Combine ("Logs", "Xamarin", flavor),
                userLibraryDirectory.Combine ("Preferences", "Xamarin", flavor),
                userLibraryDirectory.Combine ("Caches", $"com.xamarin.{flavor}"));
        }

        protected override IPreferenceStore CreatePreferenceStore ()
        {
            const string preferencesVersionKey = "preferencesVersion";
            const string inspectorBundleId = "com.xamarin.Inspector";

            var preferenceStore = new NSUserDefaultsPreferenceStore (synchronizeOnSet: true);

            // Check for, and if necessary, perform a one-time preferences migration from
            // com.xamarin.Inspector to com.xamarin.Workbooks, allowing the two client
            // applications to more fully split (they no longer share CFBundleIdentifier)
            if (ClientInfo.Flavor == ClientFlavor.Inspector ||
                preferenceStore.GetInt64 (preferencesVersionKey) >= 2)
                return preferenceStore;

            var migratedPrefs = 0;

            using (var inspectorDefaults = new NSUserDefaults ()) {
                inspectorDefaults.AddSuite (inspectorBundleId);

                foreach (var item in inspectorDefaults.ToDictionary ()) {
                    if (item.Key is NSString nsKey && item.Value != null) {
                        var key = nsKey.ToString ();
                        switch (key) {
                        // preserve some Cocoa/WebKit preferences
                        case "WebKitDeveloperExtras":
                        case "NSNavLastRootDirectory":
                        case "NSNavRecentPlaces":
                            break;
                        default:
                            // preserve all nav panel (open/save) prefs
                            if (key.StartsWith ("NSNavPanel", StringComparison.Ordinal))
                                break;

                            // lastly, preserve our own explicit preferences
                            if (key.StartsWith (inspectorBundleId + ".", StringComparison.Ordinal)) {
                                key = preferenceStore.BundleId + key.Substring (inspectorBundleId.Length);
                                break;
                            }

                            // there are tons of "junk" preferences/state stored by various
                            // controls (e.g. frame dimensions) that I'd rather just discard
                            continue;
                        }

                        preferenceStore.UserDefaults.SetValueForKey (item.Value, new NSString (key));
                        migratedPrefs++;
                    }
                }

                preferenceStore.Set (preferencesVersionKey, 2);
                preferenceStore.UserDefaults.Synchronize ();

                Log.Info (TAG, $"Migrated {migratedPrefs} preferences from " +
                    $"{inspectorBundleId} to {preferenceStore.BundleId}");
            }

            return preferenceStore;
        }

        protected override ClientAppHostEnvironment CreateHostEnvironment ()
            => new MacHostEnvironment ();

        protected override IFileSystem CreateFileSystem ()
            => new MacFileSystem ();

        protected override ClientWebServer CreateClientWebServer ()
            => new ClientWebServer (
                new FilePath (NSBundle.MainBundle.ResourcePath).Combine ("ClientApp"));

        protected override UpdaterService CreateUpdaterService ()
        {
            string updaterChannel = null;
            var visualStudio = VisualStudioForMacComponent.Installation;
            if (visualStudio != null && visualStudio.IsInstalled)
                updaterChannel = visualStudio.UpdaterChannel;

            return new UpdaterService (
                "mac",
                "42a8c70f-b3dc-42f4-b8a5-435a1bb2410c",
                updaterChannel);
        }

        protected override void OnInitialized ()
        {
            var workbookAppsDirectory = new FilePath (NSBundle.MainBundle.SharedSupportPath)
                .Combine ("WorkbookApps");
            if (workbookAppsDirectory.DirectoryExists)
                WorkbookAppInstallation.RegisterSearchPath (workbookAppsDirectory);

            WorkbookAppInstallation.RegisterPathMapper (
                WorkbookAppInstallation_MacosVersionPathMapper);

            RoutedCommandFactory.Initialize (new MacRoutedCommandFactory ());
        }

        string WorkbookAppInstallation_MacosVersionPathMapper (string path)
        {
            const string macosVersionVar = "{macosVersion}";
            var index = path.IndexOf (macosVersionVar, StringComparison.Ordinal);
            if (index < 0)
                return path;

            var versionsParentDir = path.Substring (0, index);
            if (!Directory.Exists (versionsParentDir))
                return path;

            var highestCompatibleVersionDir = Directory
                .EnumerateDirectories (versionsParentDir)
                .Select (d => Path.GetFileName (d))
                .Select (vd => new {
                    DirectoryName = vd,
                    Version = Version.TryParse (vd, out var version) ? version : null,
                })
                .Where (vp => vp.Version != null)
                .OrderByDescending (vp => vp.Version)
                .FirstOrDefault (vp => vp.Version <= Host.OSVersion);

            if (highestCompatibleVersionDir == null)
                return path;

            return path.Replace (
                macosVersionVar,
                highestCompatibleVersionDir.DirectoryName);
        }
    }
}