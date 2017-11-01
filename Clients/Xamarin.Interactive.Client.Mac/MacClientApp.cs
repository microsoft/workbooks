//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Foundation;

using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Client
{
    sealed class MacClientApp : ClientApp
    {
        sealed class MacHostEnvironment : HostEnvironment
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

            return new ClientAppPaths (
                userLibraryDirectory.Combine ("Logs", "Xamarin", "Inspector"),
                userLibraryDirectory.Combine ("Preferences", "Xamarin", "Inspector"),
                userLibraryDirectory.Combine ("Caches", "com.xamarin.Inspector"));
        }

        protected override IPreferenceStore CreatePreferenceStore ()
            => new NSUserDefaultsPreferenceStore (synchronizeOnSet: true);

        protected override HostEnvironment CreateHostEnvironment ()
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
    }
}