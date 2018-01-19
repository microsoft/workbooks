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
            var publicationItems = new List<ReleaseFile> ();

            foreach (var item in FilesToInclude) {
                if (new FileInfo (item.ItemSpec).FullName ==
                    new FileInfo (OutputFile.ItemSpec).FullName)
                    continue;

                var publicationItem = ProcessItem (item.ItemSpec, item.GetMetadata ("Evergreen"));
                if (publicationItem == null)
                    return false;

                publicationItems.Add (publicationItem);
            }

            using (var writer = new StreamWriter (OutputFile.ItemSpec))
                new Release {
                    Info = new ReleaseInfo {
                        Name = ReleaseName
                    },
                    ReleaseFiles = publicationItems
                }.Serialize (writer);

            return true;
        }

        ReleaseFile ProcessItem (string path, string evergreenName)
        {
            var item = new ReleaseFile ();
            try {
                item.PopulateFromFile (path);
            } catch (Exception e) {
                Log.LogError ($"error creating ingestion item for '{path}': {e.Message}");
                return null;
            }

            var fileName = Path.GetFileName (path);

            item.SourceUri = fileName;
            item.PublishUri = $"{RelativePublishBaseUrl}/{fileName}";

            if (!string.IsNullOrEmpty (evergreenName))
                item.EvergreenUri = $"{RelativePublishBaseUrl}/{evergreenName}";

            return ProcessItem (item);
        }

        static readonly Regex updaterFileRegex = new Regex (
            @"^(?<name>[\w-_]+)-(?<version>\d+.*)(?<extension>\.(msi|pkg|dmg))$",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        static readonly Regex pdbArchiveRegex = new Regex (
            @"\-PDB\-.+\.zip$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        ReleaseFile ProcessItem (ReleaseFile item)
        {
            var relativePath = item.PublishUri;
            var relativePathFileName = Path.GetFileName (relativePath);

            if (pdbArchiveRegex.IsMatch (relativePathFileName)) {
                item.PublishUri = null;
                return item;
            }

            var updaterItem = updaterFileRegex.Match (relativePathFileName);
            if (updaterItem == null || !updaterItem.Success)
                return item;

            if (string.IsNullOrEmpty (ReleaseName))
                ReleaseName = $"{updaterItem.Groups ["name"]}-{updaterItem.Groups ["version"]}";

            item.UpdaterProduct = new XamarinUpdaterProduct {
                Version = updaterItem.Groups ["version"].Value
            };

            if (UpdateInfoFile != null)
                item.UpdaterProduct.PopulateFromUpdateinfoFile (UpdateInfoFile);

            if (UpdaterReleaseNotes != null)
                item.UpdaterProduct.Blurb = string
                    .Join ("\n", UpdaterReleaseNotes)
                    .Trim ();

            if (ReleaseVersion.TryParse (item.UpdaterProduct.Version, out var version)) {
                if (version.CandidateLevel == ReleaseCandidateLevel.Stable)
                    item.EvergreenUri =
                        Path.GetDirectoryName (relativePath) + "/" +
                        updaterItem.Groups ["name"].Value +
                        updaterItem.Groups ["extension"].Value;

                switch (version.CandidateLevel) {
                case ReleaseCandidateLevel.Alpha:
                    item.UpdaterProduct.Channels =
                        XamarinUpdaterChannels.Alpha;
                    break;
                case ReleaseCandidateLevel.Beta:
                case ReleaseCandidateLevel.StableCandidate:
                    item.UpdaterProduct.Channels =
                        XamarinUpdaterChannels.Alpha |
                        XamarinUpdaterChannels.Beta;
                    break;
                case ReleaseCandidateLevel.Stable:
                    item.UpdaterProduct.Channels =
                        XamarinUpdaterChannels.Alpha |
                        XamarinUpdaterChannels.Beta |
                        XamarinUpdaterChannels.Stable;
                    break;
                }
            }

            return item;
        }
    }
}