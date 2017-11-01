//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.SystemInformation
{
    sealed class XamarinComponent : ISoftwareComponent
    {
        static readonly string ProgramFilesX86 = Environment.GetFolderPath (
            Environment.SpecialFolder.ProgramFilesX86);

        public string Name { get; }
        public string Version { get; }
        public string UpdaterVersion { get; }
        public bool IsInstalled { get; }

        static FilePath GetFrameworkBasePath (string name)
            => Environment.OSVersion.Platform == PlatformID.Unix
                ? $"/Library/Frameworks/Xamarin.{name}.framework/Versions/Current"
                : Path.Combine (ProgramFilesX86, "MSBuild", "Xamarin", name);

        public XamarinComponent (string name, FilePath basePath = default (FilePath))
        {
            switch (name) {
            case "VS":
                basePath = GetFrameworkBasePath (name).ParentDirectory;
                break;
            case "Simulator":
            case "Profiler":
                basePath = Path.Combine (ProgramFilesX86, "Xamarin", name);
                break;
            case "Mono":
                basePath = "/Library/Frameworks/Mono.framework/Versions/Current";
                Name = name;
                break;
            default:
                if (basePath.IsNull)
                    basePath = GetFrameworkBasePath (name);
                break;
            }

            if (Name == null)
                Name = $"Xamarin {name}";

            if (!basePath.DirectoryExists)
                return;

            var versionPath = basePath.Combine ("Version");
            if (versionPath.FileExists) {
                Version = ReadVersionFile (versionPath);
            } else {
                // Android 🤦‍♂️
                versionPath = basePath.Combine ("Version.txt");
                if (versionPath.FileExists)
                    Version = ReadVersionFile (versionPath);
            }

            var versionRevPath = basePath.Combine ("Version.rev");
            if (!string.IsNullOrEmpty (Version) && versionRevPath.FileExists)
                Version += "-" + ReadVersionFile (versionRevPath);

            var updateinfoPath = basePath.Combine ("updateinfo");
            if (!updateinfoPath.FileExists)
                updateinfoPath = basePath.Combine ("updateinfo.dat");

            if (updateinfoPath.FileExists) {
                UpdaterVersion = ReadUpdateinfoFile (updateinfoPath);
                if (string.IsNullOrWhiteSpace (Version))
                    Version = UpdaterVersion;
            }

            IsInstalled = Name != null && Version != null;
        }

        void ISoftwareComponent.SerializeExtraProperties (JsonTextWriter writer)
        {
            if (UpdaterVersion != null) {
                writer.WritePropertyName ("updaterVersion");
                writer.WriteValue (UpdaterVersion);
            }
        }

        static string ReadVersionFile (FilePath path)
        {
            try {
                return File.ReadAllText (path).Trim ();
            } catch {
                return null;
            }
        }

        public static string ReadUpdateinfoFile (FilePath path)
        {
            try {
                var updateinfo = File.ReadAllText (path).Trim ();
                if (string.IsNullOrWhiteSpace (updateinfo))
                    return null;

                var parts = updateinfo.Split (
                    new [] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts == null || parts.Length != 2)
                    return null;

                return string.IsNullOrWhiteSpace (parts [1]) ? null : parts [1];
            } catch {
                return null;
            }
        }
    }
}