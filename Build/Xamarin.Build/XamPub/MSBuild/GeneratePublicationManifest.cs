//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;

using Xamarin.Versioning;

using Xamarin.XamPub.Models;

namespace Xamarin.XamPub.MSBuild
{
    public sealed class GeneratePublicationManifest : Task
    {
        [Required]
        public ITaskItem OutputFile { get; private set; }

        public string UpdateInfoFile { get; set; }

        public string [] UpdaterReleaseNotes { get; set; }

        [Required]
        public ITaskItem [] FilesToInclude { get; set; }

        [Required]
        public string RelativePublishBaseUrl { get; set; }

        public string ReleaseName { get; set; }

        public override bool Execute ()
        {
            var releaseFiles = new List<ReleaseFile> ();

            foreach (var item in FilesToInclude) {
                if (new FileInfo (item.ItemSpec).FullName ==
                    new FileInfo (OutputFile.ItemSpec).FullName)
                    continue;

                var releaseFile = ProcessReleaseFile (item.ItemSpec, item.GetMetadata ("Evergreen"));
                if (releaseFile == null)
                    return false;

                releaseFiles.Add (releaseFile);
            }

            using (var writer = new StreamWriter (OutputFile.ItemSpec))
                new Release {
                    Info = new ReleaseInfo {
                        Name = ReleaseName
                    },
                    ReleaseFiles = releaseFiles
                }.Serialize (writer);

            return true;
        }

        ReleaseFile ProcessReleaseFile (string path, string evergreenName)
        {
            var releaseFile = new ReleaseFile ();
            try {
                releaseFile.PopulateFromFile (path);
            } catch (Exception e) {
                Log.LogError ($"error creating ingestion item for '{path}': {e.Message}");
                return null;
            }

            var fileName = Path.GetFileName (path);

            releaseFile.SourceUri = fileName;
            releaseFile.PublishUri = $"{RelativePublishBaseUrl}/{fileName}";

            if (!string.IsNullOrEmpty (evergreenName))
                releaseFile.EvergreenUri = $"{RelativePublishBaseUrl}/{evergreenName}";

            return ProcessReleaseFile (releaseFile);
        }

        static readonly Regex updaterFileRegex = new Regex (
            @"^(?<name>[\w-_]+)-(?<version>\d+.*)(?<extension>\.(msi|pkg|dmg))$",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        static readonly Regex pdbArchiveRegex = new Regex (
            @"\-PDB\-.+\.zip$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        ReleaseFile ProcessReleaseFile (ReleaseFile releaseFile)
        {
            var relativePath = releaseFile.PublishUri;
            var relativePathFileName = Path.GetFileName (relativePath);

            if (pdbArchiveRegex.IsMatch (relativePathFileName)) {
                releaseFile.PublishUri = null;
                return releaseFile;
            }

            var updaterItem = updaterFileRegex.Match (relativePathFileName);
            if (updaterItem == null || !updaterItem.Success)
                return releaseFile;

            if (string.IsNullOrEmpty (ReleaseName))
                ReleaseName = $"{updaterItem.Groups ["name"]}-{updaterItem.Groups ["version"]}";

            releaseFile.UpdaterProduct = new XamarinUpdaterProduct {
                Version = updaterItem.Groups ["version"].Value
            };

            if (UpdateInfoFile != null)
                releaseFile.UpdaterProduct.PopulateFromUpdateinfoFile (UpdateInfoFile);

            if (UpdaterReleaseNotes != null)
                releaseFile.UpdaterProduct.Blurb = string
                    .Join ("\n", UpdaterReleaseNotes)
                    .Trim ();

            if (ReleaseVersion.TryParse (releaseFile.UpdaterProduct.Version, out var version)) {
                if (version.CandidateLevel == ReleaseCandidateLevel.Stable)
                    releaseFile.EvergreenUri =
                        Path.GetDirectoryName (relativePath) + "/" +
                        updaterItem.Groups ["name"].Value +
                        updaterItem.Groups ["extension"].Value;

                switch (version.CandidateLevel) {
                case ReleaseCandidateLevel.Alpha:
                    releaseFile.UpdaterProduct.Channels =
                        XamarinUpdaterChannels.Alpha;
                    break;
                case ReleaseCandidateLevel.Beta:
                case ReleaseCandidateLevel.StableCandidate:
                    releaseFile.UpdaterProduct.Channels =
                        XamarinUpdaterChannels.Alpha |
                        XamarinUpdaterChannels.Beta;
                    break;
                case ReleaseCandidateLevel.Stable:
                    releaseFile.UpdaterProduct.Channels =
                        XamarinUpdaterChannels.Alpha |
                        XamarinUpdaterChannels.Beta |
                        XamarinUpdaterChannels.Stable;
                    break;
                }
            }

            return releaseFile;
        }
    }
}