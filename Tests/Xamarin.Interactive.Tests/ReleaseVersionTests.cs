//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using NUnit.Framework;

using Should;

using Xamarin.Versioning;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public sealed class ReleaseVersionTests
    {
        [TestCase ("0.0.0", "0.0.0.9000", null, null)]
        [TestCase ("0.99.0-beta3+6692", "0.99.0.5003", null, null)]
        [TestCase ("1.2.0", "1.2.0.9000", "1.2", "1.2")]
        [TestCase ("1.2.3", "1.2.3.9000", "1.2.3", "1.2.3")]
        [TestCase ("99.99.99-rc99+9999", "99.99.99.7099", "99.99.99fc99", "99.99.99")]
        [TestCase ("99.99.99", "99.99.99.9000", "99.99.99", "99.99.99")]
        [TestCase ("3.53.8-alpha", "3.53.8.3000", "3.53.8a0", "3.53.8")]
        [TestCase ("3.53.8-alpha26", "3.53.8.3026", "3.53.8a26", "3.53.8")]
        [TestCase ("3.53.0-rc1", "3.53.0.7001", "3.53fc1", "3.53")]
        public void ValidVersion (
            string expectedSemVer,
            string expectedWindowsFileVersion,
            string expectedCFBundleVersion,
            string expectedCFBundleShortVersionString)
        {
            ReleaseVersion version;

            // parse and assert the semver format
            ReleaseVersion.TryParse (expectedSemVer, out version).ShouldBeTrue ();
            version.ToString ().ShouldEqual (expectedSemVer);

            // parse and assert the windows file version format
            var windowsFileVersion = version.ToWindowsFileVersion ();
            windowsFileVersion.ToString ().ShouldEqual (expectedWindowsFileVersion);
            version.WithBuild (0).ShouldEqual (ReleaseVersion.FromWindowsFileVersion (windowsFileVersion));

            if (expectedCFBundleVersion == null)
                Assert.Throws<FormatException> (
                    () => version.ToString (ReleaseVersionFormat.AppleCFBundleVersion));
            else
                version.ToString (ReleaseVersionFormat.AppleCFBundleVersion)
                    .ShouldEqual (expectedCFBundleVersion);

            if (expectedCFBundleShortVersionString == null)
                Assert.Throws<FormatException> (
                    () => version.ToString (ReleaseVersionFormat.AppleCFBundleShortVersion));
            else
                version.ToString (ReleaseVersionFormat.AppleCFBundleShortVersion)
                    .ShouldEqual (expectedCFBundleShortVersionString);
        }

        [TestCase ("1.2.0-alpha.1", "1.2.0-alpha1")]
        [TestCase ("1.2.3+build.99", "1.2.3+99")]
        [TestCase ("1.2.3-rc3+build.99", "1.2.3-rc3+99")]
        [TestCase ("1.2.3-rc.10+build.3000", "1.2.3-rc10+3000")]
        public void VerboseParse (string longerSemVerForm, string shorterSemVerForm)
            => ReleaseVersion
                .Parse (longerSemVerForm)
                .ToString ()
                .ShouldEqual (shorterSemVerForm);

        [TestCase ("1.2.0", "1.2", false)]
        [TestCase ("1.4.0-alpha1+3", "1.4 Alpha 1", false)]
        [TestCase ("1.4.0-alpha1+3", "1.4 Alpha 1 Build 3", true)]
        [TestCase ("1.4.0-rc4", "1.4 RC 4", false)]
        [TestCase ("1.4.3-rc4+938", "1.4.3 RC 4 Build 938", true)]
        public void Friendly (string semVer, string expectedFriendly, bool withBuildComponent)
            => ReleaseVersion
                .Parse (semVer)
                .ToString (ReleaseVersionFormat.FriendlyShort, withBuildComponent)
                .ShouldEqual (expectedFriendly);

        [TestCase ("1.4.0-alpha1", "1.4a1")]
        [TestCase ("1.4.1-alpha2", "1.4.1a2")]
        [TestCase ("1.4.2-alpha3", "1.4.2a3")]
        [TestCase ("1.4.0-beta99", "1.4b99")]
        [TestCase ("1.4.1-beta99", "1.4.1b99")]
        [TestCase ("1.4.2-beta19", "1.4.2b19")]
        [TestCase ("1.4.0-rc1", "1.4fc1")]
        [TestCase ("1.4.0-rc25", "1.4fc25")]
        [TestCase ("1.4.1", "1.4.1")]
        [TestCase ("1.4.2+99", "1.4.2")]
        [TestCase ("1.4.0+1", "1.4")]
        [TestCase ("1.4.0-dev+188", "1.4d188")]
        [TestCase ("1.4.1-local+1", "1.4.1d1")]
        public void CFBundleVersion (string semVer, string expectedCFBundleVersion)
            => ReleaseVersion
                .Parse (semVer)
                .ToString (ReleaseVersionFormat.AppleCFBundleVersion)
                .ShouldEqual (expectedCFBundleVersion);

        [Test]
        public void Precedence ()
        {
            string [] versions = {
                "0.0.0-beta.1",
                "0.0.0-beta.1+build.1",
                "0.0.0-beta.1+build.2",
                "0.0.0-beta.2",
                "0.0.0-beta.2+build.1",
                "0.0.0-beta.2+build.2",
                "0.0.0",
                "0.1.0",
                "0.1.1-dev",
                "0.1.1-alpha",
                "0.1.1-beta",
                "0.1.1-rc",
                "0.1.1-rc.1",
                "0.1.1-rc.2",
                "0.1.1-rc.3",
                "0.1.1-rc.3+build.1",
                "0.1.1",
                "1.0.1",
                "1.2.0-rc.2",
                "1.2.0-rc.3",
                "1.2.0-rc4",
                "1.2.0",
                "1.2.1-rc.9",
                "1.2.1",
                "1.4.0-alpha1",
                "1.4.0-alpha2",
                "1.4.0-alpha3",
                "1.4.0-beta1",
                "1.4.0-beta2",
                "1.4.0-beta.3",
                "1.4.0-rc",
                "1.4.0-rc1",
                "1.4.0-rc2",
                "1.4.0"
            };

            for (int i = 1; i < versions.Length; i++) {
                var older = ReleaseVersion.Parse (versions [i - 1]);
                var newer = ReleaseVersion.Parse (versions [i]);
                newer.ShouldBeGreaterThan (older);
                older.ShouldBeLessThan (newer);

                // Windows file version test does an equal to as well since
                // the conversion drops the build component, which makes
                // some of the versions we check equal.
                var olderSV = older.ToWindowsFileVersion ();
                var newerSV = newer.ToWindowsFileVersion ();
                newerSV.ShouldBeGreaterThanOrEqualTo (olderSV);
                olderSV.ShouldBeLessThanOrEqualTo (newerSV);
            }
        }

        [Test]
        public void Equality ()
        {
            ReleaseVersion [] versions = {
                ReleaseVersion.Parse ("0.0.0"),
                new ReleaseVersion (0, 0, 0),

                ReleaseVersion.Parse ("99.99.99"),
                new ReleaseVersion (99, 99, 99),

                ReleaseVersion.Parse ("99.99.99-rc.99+build.9999"),
                new ReleaseVersion (99, 99, 99, ReleaseCandidateLevel.StableCandidate, 99, 9999),

                ReleaseVersion.Parse ("0.99.0-beta.3+build.6692"),
                new ReleaseVersion (0, 99, 0, ReleaseCandidateLevel.Beta, 3, 6692)
            };

            for (int i = 0; i < versions.Length; i += 2)
                versions [i].ShouldEqual (versions [i + 1]);
        }
    }
}