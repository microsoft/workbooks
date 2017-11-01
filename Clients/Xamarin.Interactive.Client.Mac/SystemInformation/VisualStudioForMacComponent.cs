//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;

using AppKit;
using Foundation;
using Newtonsoft.Json;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.SystemInformation
{
    sealed class VisualStudioForMacComponent : ISoftwareComponent
    {
        const string TAG = nameof (VisualStudioForMacComponent);

        public const string XSBundleId = "com.xamarin.xamarinstudio";
        public const string VSMacBundleId = "com.microsoft.visual-studio";

        static readonly Version firstStableVSMVersion = new Version (7, 0, 0, 3146);

        static readonly VisualStudioForMacComponent xamarinStudio
            = new VisualStudioForMacComponent (XSBundleId);

        static readonly VisualStudioForMacComponent visualStudioForMac
            = new VisualStudioForMacComponent (VSMacBundleId);

        static VisualStudioForMacComponent installation;
        public static VisualStudioForMacComponent Installation {
            get {
                if (installation == null) {
                    if (visualStudioForMac.IsInstalled &&
                        visualStudioForMac.PreferencesDirectory.DirectoryExists)
                        installation = visualStudioForMac;
                    else
                        installation = xamarinStudio;
                }

                return installation;
            }
        }

        readonly string bundleId;

        public NSBundle Bundle { get; private set; }

        public string Name { get; private set; }
        public string Version { get; private set; }
        public bool IsInstalled { get; private set; }

        public FilePath AppPath { get; private set; }
        public FilePath PreferencesDirectory { get; private set; }

        readonly Lazy<Dictionary<string, string>> properties;
        public IReadOnlyDictionary<string, string> Properties => properties.Value;

        string updaterChannel;
        public string UpdaterChannel {
            get {
                if (updaterChannel != null)
                    return updaterChannel;

                if (!Properties.TryGetValue (
                    "MonoDevelop.Ide.AddinUpdater.UpdateLevel",
                    out updaterChannel) || updaterChannel == null)
                    updaterChannel = "Stable";

                return updaterChannel;
            }
        }

        public VisualStudioForMacComponent (string bundleId)
        {
            this.bundleId = bundleId;

            Name = bundleId;

            properties = new Lazy<Dictionary<string, string>> (
                LoadProperties,
                LazyThreadSafetyMode.PublicationOnly);

            try {
                Locate ();
            } catch (Exception e) {
                Log.Error (TAG, e);
            }
        }

        void ISoftwareComponent.SerializeExtraProperties (JsonTextWriter writer)
        {
        }

        Dictionary<string, string> LoadProperties ()
        {
            var dict = new Dictionary<string, string> ();

            try {
                if (!PreferencesDirectory.DirectoryExists)
                    return dict;

                var monodevelopPropertiesPath = PreferencesDirectory
                    .Combine ("MonoDevelopProperties.xml");

                if (!monodevelopPropertiesPath.FileExists) {
                    Log.Warning (TAG, $"{monodevelopPropertiesPath} does not exist");
                    return dict;
                }

                using (var reader = XmlReader.Create (monodevelopPropertiesPath)) {
                    while (reader.Read ()) {
                        if (reader.NodeType == XmlNodeType.Element &&
                            reader.Name == "Property") {
                            var key = reader.GetAttribute ("key");
                            var value = reader.GetAttribute ("value");
                            if (key != null && value != null)
                                dict [key] = value;
                        }
                    }
                }
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            return dict;
        }

        void Locate ()
        {
            var bundlePath = NSWorkspace.SharedWorkspace.AbsolutePathForAppBundle (bundleId);
            if (bundlePath == null)
                return;

            Bundle = NSBundle.FromPath (bundlePath);
            if (Bundle == null)
                return;

            AppPath = Bundle.BundlePath;

            if (!Bundle.InfoDictionary.TryGetValue (
                new NSString ("CFBundleName"), out var nameOut) || nameOut == null)
                return;

            Name = nameOut.ToString ();

            if (!Bundle.InfoDictionary.TryGetValue (
                new NSString ("CFBundleVersion"), out var versionOut) || versionOut == null)
                return;

            Version = versionOut.ToString ();
            System.Version.TryParse (Version, out var sysVersion);

            var updateinfoPath = new FilePath (bundlePath).Combine ("Contents", "MacOS", "updateinfo");
            if (updateinfoPath.FileExists) {
                var updateinfo = XamarinComponent.ReadUpdateinfoFile (updateinfoPath);
                if (updateinfo != null)
                    Version += $" ({updateinfo})";
            }

            var libraryUrl = NSFileManager.DefaultManager.GetUrl (
                NSSearchPathDirectory.LibraryDirectory,
                NSSearchPathDomain.User,
                null,
                false,
                out var error);

            if (error != null)
                throw new NSErrorException (error);

            if (libraryUrl == null)
                throw new DirectoryNotFoundException ("unable to locate user Library directory");

            if (sysVersion == null)
                return;

            var isVsm = bundleId == VSMacBundleId;

            // Do not entertain any of the very first VSM preview releases
            if (isVsm && sysVersion < firstStableVSMVersion)
                return;

            foreach (var compatVersion in new [] {
                $"{sysVersion.Major}.{sysVersion.Minor}",
                $"{sysVersion.Major}.0" }) {
                if (isVsm)
                    PreferencesDirectory = new FilePath (libraryUrl.Path).Combine (
                        "Preferences",
                        "VisualStudio",
                        compatVersion);
                else
                    PreferencesDirectory = new FilePath (libraryUrl.Path).Combine (
                        "Preferences",
                        $"XamarinStudio-{compatVersion}");

                if (PreferencesDirectory.DirectoryExists)
                    break;
            }

            IsInstalled = AppPath.DirectoryExists && !string.IsNullOrEmpty (Version);
        }

        public void Launch ()
        {
            Log.Info (TAG, $"Launching {Name} {Version}: {Bundle.BundleUrl}");
            NSWorkspace.SharedWorkspace.OpenURL (
                Bundle.BundleUrl,
                NSWorkspaceLaunchOptions.Default,
                new NSDictionary (),
                out var error);
            if (error != null)
                throw new Exception (error.LocalizedDescription);
        }
    }
}