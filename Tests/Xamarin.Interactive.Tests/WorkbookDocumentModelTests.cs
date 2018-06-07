//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public class WorkbookDocumentManifestTests
    {
        struct ReadResult
        {
            public string Source { get; set; }
            public WorkbookPage Page { get; set; }
        }

        ReadResult ReadWorkbookPage (string resourceName)
        {
            var source = new StreamReader (TestHelpers.GetResource (resourceName)).ReadToEnd ();

            var workbook = new WorkbookPage (new WorkbookPackage ());
            workbook.Read (new StringReader (source));

            return new ReadResult { Source = source, Page = workbook };
        }

        [TestCase ("LegacyJsonManifest.workbook")]
        [TestCase ("LegacyJsonManifest-no-exec-mode.workbook")]
        public void TestLegacyManifest (string docFile)
        {
            var page = ReadWorkbookPage (docFile).Page;
            var packages = page.PackageReferences;

            page.PlatformTargets.Single ().ShouldEqual (AgentType.iOS);

            packages.Count.ShouldEqual (4);

            packages
                .Any (p => p.PackageId == "Newtonsoft.Json" && p.VersionRange == "8.0.3")
                .ShouldBeTrue ();

            packages
                .Any (p => p.PackageId == "Microsoft.Net.Http" && p.VersionRange == "2.2.29")
                .ShouldBeTrue ();

            packages
                .Any (p => p.PackageId == "Microsoft.Azure.Mobile.Client" && p.VersionRange == "2.0.1")
                .ShouldBeTrue ();

            packages
                .Any (p => p.PackageId == "Microsoft.Bcl" && p.VersionRange == "1.1.10")
                .ShouldBeTrue ();
        }

        [Test]
        public void BareMinimumYAMLManifest ()
            => Assert.DoesNotThrow (() => ReadWorkbookPage ("BareMinimumYAMLManifest.workbook"));

        [Test]
        public void TestPackageVersionRanges ()
        {
            var packages = ReadWorkbookPage ("ArbitraryYAMLManifest.workbook")
                .Page
                .PackageReferences;

            packages.Count.ShouldEqual (4);

            var expectedPackages = new [] {
                new { id = "Fake.Package1", originalRange = "9.0.1" },
                new { id = "Fake.Package2", originalRange = "[9.0.1]" },
                new { id = "Fake.Package3", originalRange = "9.0.*" },
                new { id = "Fake.Package4", originalRange = "[9,10)" },
            };

            foreach (var expected in expectedPackages) {
                var package = packages.Single (p => PackageIdComparer.Equals (p.PackageId, expected.id));
                package.VersionRange.ShouldEqual (expected.originalRange);
            }
        }

        [Test]
        public void TestWritingNewPackages ()
        {
            var page = new WorkbookPage (new WorkbookPackage ());
            var packageVersion = "9.0.1";
            var package = new InteractivePackageDescription ("Fake.Package1", versionRange: packageVersion);
            page.Manifest.PackageReferences = page.Manifest.PackageReferences.AddOrUpdate (package);

            var writer = new StringWriter ();
            page.Write (writer, null);
            var serializedPage = writer.ToString ();

            var deserializedPage = new WorkbookPage (new WorkbookPackage ());
            deserializedPage.Read (new StringReader (serializedPage));

            deserializedPage.PackageReferences.Count.ShouldEqual (1);

            var deserializedPackage = deserializedPage.PackageReferences [0];
            deserializedPackage.PackageId.ShouldEqual (package.PackageId);
            // Make sure this doesn't serialize as [9.0.1, )
            deserializedPackage.VersionRange.ShouldEqual (packageVersion);
        }

        [Test]
        public void ArbitraryYAMLManifest ()
        {
            var result = ReadWorkbookPage ("ArbitraryYAMLManifest.workbook");

            result.Page.Manifest.PlatformTargets.Length.ShouldEqual (1);
            result.Page.Manifest.PlatformTargets [0].ShouldEqual (AgentType.iOS);

            object propertyValue;

            result.Page.Manifest.Properties.TryGetValue ("ms.author", out propertyValue).ShouldBeTrue ();
            propertyValue.ShouldBeInstanceOf<string> ().ShouldEqual ("abock");

            result.Page.Manifest.Properties.TryGetValue ("author", out propertyValue).ShouldBeTrue ();
            propertyValue.ShouldBeInstanceOf<string> ().ShouldEqual ("Aaron Bockover");

            var writer = new StringWriter ();
            result.Page.Write (writer, null);
            writer.ToString ().ShouldEqual (result.Source, ShouldEqualOptions.LineDiff);
        }

        [TestCase ("LotsOfPlatformYAML.workbook")]
        [TestCase ("LotsOfPlatformsYAML.workbook")]
        public void YAMLTargetPlatforms (string resourceName)
        {
            var platforms = ReadWorkbookPage (resourceName).Page.Manifest.PlatformTargets;
            platforms.Length.ShouldEqual (5);
            platforms = platforms.Sort ();
            platforms [0].ShouldEqual (AgentType.iOS);
            platforms [1].ShouldEqual (AgentType.MacMobile);
            platforms [2].ShouldEqual (AgentType.Android);
            platforms [3].ShouldEqual (AgentType.WPF);
            platforms [4].ShouldEqual (AgentType.Console);
        }
    }
}