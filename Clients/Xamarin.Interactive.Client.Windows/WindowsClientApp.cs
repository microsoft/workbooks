//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.Win32;

using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.IO.Windows;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Client
{
    sealed class WindowsClientApp : ClientApp
    {
        const string TAG = nameof (WindowsClientApp);

        sealed class WindowsHostEnvironment : ClientAppHostEnvironment
        {
            public override HostOS OSName { get; } = HostOS.Windows;

            public override string OSVersionString { get; } = Environment.OSVersion.VersionString;

            public override Version OSVersion { get; } = Environment.OSVersion.Version;

            public override int? ProcessorCount => Environment.ProcessorCount;

            public override ulong? PhysicalMemory => MEMORYSTATUSEX.Get ()?.ullTotalPhys;

            public WindowsHostEnvironment () : base (LoadSoftwareEnvironmentsAsync)
            {
            }

            static async Task<IReadOnlyList<ISoftwareEnvironment>> LoadSoftwareEnvironmentsAsync ()
            {
                var environments = new List<ISoftwareEnvironment> {
                    new SystemSoftwareEnvironment {
                        new XamarinComponent ("iOS"),
                        new XamarinComponent ("Android"),
                        new XamarinComponent ("Simulator")
                    }
                };

                environments.AddRange (await VisualStudioEnvironment.GetInstallations ());

                return environments;
            }

            [StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
            sealed class MEMORYSTATUSEX
            {
                public uint dwLength;
                public uint dwMemoryLoad;
                public ulong ullTotalPhys;
                public ulong ullAvailPhys;
                public ulong ullTotalPageFile;
                public ulong ullAvailPageFile;
                public ulong ullTotalVirtual;
                public ulong ullAvailVirtual;
                public ulong ullAvailExtendedVirtual;

                MEMORYSTATUSEX ()
                    => dwLength = (uint)Marshal.SizeOf (typeof (MEMORYSTATUSEX));

                [DllImport ("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
                [return: MarshalAs (UnmanagedType.Bool)]
                static extern bool GlobalMemoryStatusEx ([In, Out] MEMORYSTATUSEX lpBuffer);

                public static MEMORYSTATUSEX Get ()
                {
                    try {
                        var status = new MEMORYSTATUSEX ();
                        if (GlobalMemoryStatusEx (status))
                            return status;
                    } catch {
                    }

                    return null;
                }
            }
        }

        protected override ClientAppPaths CreateClientAppPaths ()
        {
            FilePath appData = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
            FilePath localAppData = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);

            var flavor = ClientInfo.Flavor.ToString ();

            return new ClientAppPaths (
                localAppData.Combine ("Xamarin", flavor, "logs"),
                appData.Combine ("Xamarin", flavor),
                localAppData.Combine ("Xamarin", flavor, "Cache"));
        }

        protected override ClientAppHostEnvironment CreateHostEnvironment ()
            => new WindowsHostEnvironment ();

        protected override IPreferenceStore CreatePreferenceStore ()
        {
            const string preferencesVersionKey = "preferencesVersion";

            var preferenceStore = new RegistryPreferenceStore (
                RegistryHive.CurrentUser,
                RegistryView.Registry32,
                $@"Software\Xamarin\{ClientInfo.Flavor}\Preferences");

            // Check for, and if necessary, perform a one-time migration from Inspector to Workbooks
            if (ClientInfo.Flavor == ClientFlavor.Inspector ||
                preferenceStore.GetInt64 (preferencesVersionKey) >= 2)
                return preferenceStore;

            // Copy registry prefs
            var inspectorPreferenceStore = new RegistryPreferenceStore (
                RegistryHive.CurrentUser,
                RegistryView.Registry32,
                @"Software\Xamarin\Inspector\Preferences");

            // Copy registry prefs
            inspectorPreferenceStore.CopyTo (preferenceStore);

            preferenceStore.Set (preferencesVersionKey, 2);

            // Copy recent documents
            var inspectorRecentDocuments = FilePath.Build (
                Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
                "Xamarin",
                "Inspector",
                "recent.yaml");
            var workbooksRecentDocuments = Paths.PreferencesDirectory.Combine (inspectorRecentDocuments.Name);

            try {
                if (!File.Exists (workbooksRecentDocuments) && File.Exists (inspectorRecentDocuments)) {
                    Directory.CreateDirectory (Paths.PreferencesDirectory);
                    File.Copy (
                        inspectorRecentDocuments,
                        workbooksRecentDocuments);
                }
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            return preferenceStore;
        }

        protected override IFileSystem CreateFileSystem ()
            => new WindowsFileSystem ();

        protected override ClientWebServer CreateClientWebServer ()
            => new ClientWebServer (
                new FilePath (Assembly.GetEntryAssembly ().Location)
                    .ParentDirectory
                    .Combine ("ClientApp"));

        protected override UpdaterService CreateUpdaterService ()
            => new UpdaterService ("win", "201185fb-fefe-4996-bdfe-4b6ac311a73b");

        protected override void OnInitialized ()
        {
            var workbookAppsDirectory = new FilePath (Assembly.GetExecutingAssembly ().Location)
                .ParentDirectory
                .ParentDirectory
                .Combine ("WorkbookApps");

            if (workbookAppsDirectory.DirectoryExists)
                WorkbookAppInstallation.RegisterSearchPath (workbookAppsDirectory);

            RoutedCommandFactory.Initialize (new WpfRoutedCommandFactory ());
        }
    }
}