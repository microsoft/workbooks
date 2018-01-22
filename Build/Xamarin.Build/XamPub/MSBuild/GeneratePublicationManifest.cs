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
        public string BasePublishUri { get; set; }

        [Required]
        public string ReleaseName { get; set; }

        [Required]
        public string ReleaseDescription { get; set; }

        [Required]
        public string ProductName { get; set; }

        readonly string gitRepo = Environment.GetEnvironmentVariable ("BUILD_REPOSITORY_URI");
        readonly string gitRev = Environment.GetEnvironmentVariable ("BUILD_SOURCEVERSION");
        readonly string gitBranch = Environment.GetEnvironmentVariable ("BUILD_SOURCEBRANCH");

        public override bool Execute ()
        {
            var releaseFiles = new List<ReleaseFile> ();

            foreach (var item in FilesToInclude) {
                if (new FileInfo (item.ItemSpec).FullName ==
                    new FileInfo (OutputFile.ItemSpec).FullName)
                    continue;

                var releaseFile = ProcessFileBase<ReleaseFile> (
                    item.ItemSpec,
                    item.GetMetadata ("Evergreen"));

                if (releaseFile == null)
                    return false;

                releaseFiles.Add (releaseFile);
            }

            var releaseInfo = new ReleaseInfo {
                Name = ReleaseName,
                Description = ReleaseDescription
            };

            // if only VSTS would put this in the build environment 🙄
            var buildRequestedForEmail = Environment.GetEnvironmentVariable ("BUILD_REQUESTEDFOREMAIL");
            if (!string.IsNullOrEmpty (buildRequestedForEmail)) {
                var match = Regex.Match (
                    buildRequestedForEmail,
                    @"^(?<alias>[a-z]{1,8})@microsoft.com$",
                    RegexOptions.IgnoreCase);
                if (match.Success) {
                    releaseInfo.Alias = match.Groups["alias"].Value;
                    releaseInfo.Region = "NORTHAMERICA"; // yep
                } else {
                    Log.LogError ("Unable to discern Microsoft alias from BUILD_REQUESTEDFOREMAIL");
                }
            }

            using (var writer = new StreamWriter (OutputFile.ItemSpec))
                new Release {
                    Info = releaseInfo,
                    ReleaseFiles = releaseFiles
                }.Serialize (writer);

            return true;
        }

        TFile ProcessFileBase<TFile> (string path, string evergreenName = null)
            where TFile : FileBase, new ()
        {
            var file = new TFile ();
            try {
                file.PopulateFromFile (path);
            } catch (Exception e) {
                Log.LogError ($"error creating ingestion item for '{path}': {e.Message}");
                return null;
            }

            var fileName = Path.GetFileName (path);

            file.SourceUri = fileName;

            var releaseFile = file as ReleaseFile;
            if (releaseFile == null)
                return file;

            releaseFile.Git = new GitSource {
                Repository = gitRepo,
                Revision = gitRev,
                Branch = gitBranch
            };

            releaseFile.PublishUri = $"{BasePublishUri}/{fileName}";

            if (!string.IsNullOrEmpty (evergreenName))
                releaseFile.EvergreenUri = $"{BasePublishUri}/{evergreenName}";

            var pdbPath = path + ".symbols.zip";
            if (File.Exists (pdbPath))
                releaseFile.SymbolFiles = new List<SymbolFile> {
                    ProcessFileBase<SymbolFile> (pdbPath)
                };

            releaseFile.UploadEnvironments = UploadEnvironments.ROQ;

            ProcessReleaseFile (releaseFile);

            return file;
        }

        static readonly Regex updaterFileRegex = new Regex (
            @"^(?<name>[\w-_]+)-(?<version>\d+.*)(?<extension>\.(msi|pkg|dmg))$",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        void ProcessReleaseFile (ReleaseFile releaseFile)
        {
            var relativePath = releaseFile.PublishUri;
            var relativePathFileName = Path.GetFileName (relativePath);

            var updaterItem = updaterFileRegex.Match (relativePathFileName);
            if (updaterItem == null || !updaterItem.Success)
                return;

            releaseFile.ProductType = 11; // no idea!
            releaseFile.ProductName = ProductName;

            releaseFile.UpdaterProduct = new XamarinUpdaterProduct {
                Version = updaterItem.Groups ["version"].Value
            };

            if (UpdateInfoFile != null)
                releaseFile.UpdaterProduct.PopulateFromUpdateinfoFile (UpdateInfoFile);

            if (UpdaterReleaseNotes != null)
                releaseFile.UpdaterProduct.Blurb = string
                    .Join ("\n", UpdaterReleaseNotes)
                    .Trim ();

            if (!ReleaseVersion.TryParse (releaseFile.UpdaterProduct.Version, out var version))
                return;

            if (version.CandidateLevel == ReleaseCandidateLevel.Stable) {
                var fileExtension = updaterItem.Groups ["extension"].Value;

                releaseFile.EvergreenUri =
                    Path.GetDirectoryName (relativePath) + "/" +
                    updaterItem.Groups ["name"].Value +
                    fileExtension;

                if (fileExtension == ".pkg")
                    releaseFile.UploadEnvironments |= UploadEnvironments.XamarinInstaller;
            }

            releaseFile.UploadEnvironments |= UploadEnvironments.XamarinUpdater;

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
    }
}