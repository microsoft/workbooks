//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class ReleaseVersion : Task
    {
        [Required, Output]
        public string SemVer { get; set; }

        [Output]
        public string SemVerNuGetSafe { get; private set; }

        [Output]
        public string SemVerWithoutBuild { get; private set; }

        [Output]
        public string AppleCFBundleVersion { get; private set; }

        [Output]
        public string AppleCFBundleShortVersion { get; private set; }

        [Output]
        public string WindowsFileVersion { get; private set; }

        [Output]
        public string FriendlyLong { get; private set; }

        [Output]
        public string FriendlyShort { get; private set; }

        [Output]
        public string CandidateLevel { get; private set; }

        public override bool Execute ()
        {
            Versioning.ReleaseVersion semVer;
            if (!Versioning.ReleaseVersion.TryParse (SemVer, out semVer)) {
                Log.LogError ($"Unable to parse ReleaseVersion: {SemVer}");
                return false;
            }

            SemVer = semVer.ToString (
                Versioning.ReleaseVersionFormat.SemVer,
                withBuildComponent: true,
                nugetSafeBuild: false);

            SemVerNuGetSafe = semVer.ToString (
                Versioning.ReleaseVersionFormat.SemVer,
                withBuildComponent: true,
                nugetSafeBuild: true);

            SemVerWithoutBuild = semVer.ToString (
                Versioning.ReleaseVersionFormat.SemVer,
                withBuildComponent: false);

            AppleCFBundleVersion = semVer.ToString (
                Versioning.ReleaseVersionFormat.AppleCFBundleVersion);

            AppleCFBundleShortVersion = semVer.ToString (
                Versioning.ReleaseVersionFormat.AppleCFBundleShortVersion);

            WindowsFileVersion = semVer.ToString (
                Versioning.ReleaseVersionFormat.WindowsFileVersion);

            FriendlyLong = semVer.ToString (
                Versioning.ReleaseVersionFormat.FriendlyLong);

            FriendlyShort = semVer.ToString (
                Versioning.ReleaseVersionFormat.FriendlyShort);

            CandidateLevel = semVer.CandidateLevel.ToString ();

            return true;
        }
    }
}