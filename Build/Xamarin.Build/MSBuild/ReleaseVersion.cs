//
// ReleaseVersion.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class ReleaseVersion : Task
    {
        [Required, Output]
        public string SemVer { get; set; }

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

        public override bool Execute ()
        {
            Versioning.ReleaseVersion semVer;
            if (!Versioning.ReleaseVersion.TryParse (SemVer, out semVer)) {
                Log.LogError ($"Unable to parse ReleaseVersion: {SemVer}");
                return false;
            }

            SemVer = semVer.ToString (
                Versioning.ReleaseVersionFormat.SemVer,
                withBuildComponent: true);

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

            return true;
        }
    }
}