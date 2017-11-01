//
// WorkbookDocumentModelTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Should;

using NuGet.Versioning;

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

            page.PlatformTargets.Single ().ShouldEqual (AgentType.iOS);

            page.Packages.Length.ShouldEqual (4);

            page.Packages
                .Any (p => p.Identity.Id == "Newtonsoft.Json" && p.SupportedVersionRange.OriginalString == "8.0.3")
                .ShouldBeTrue ();

            page.Packages
                .Any (p => p.Identity.Id == "Microsoft.Net.Http" && p.SupportedVersionRange.OriginalString == "2.2.29")
                .ShouldBeTrue ();

            page.Packages
                .Any (p => p.Identity.Id == "Microsoft.Azure.Mobile.Client" && p.SupportedVersionRange.OriginalString == "2.0.1")
                .ShouldBeTrue ();

            page.Packages
                .Any (p => p.Identity.Id == "Microsoft.Bcl" && p.SupportedVersionRange.OriginalString == "1.1.10")
                .ShouldBeTrue ();
        }

        [Test]
        public void BareMinimumYAMLManifest ()
            => Assert.DoesNotThrow (() => ReadWorkbookPage ("BareMinimumYAMLManifest.workbook"));

        [Test]
        public void TestPackageVersionRanges ()
        {
            var packages = ReadWorkbookPage ("ArbitraryYAMLManifest.workbook").Page.Packages;

            packages.Length.ShouldEqual (4);

            var expectedPackages = new [] {
                new { id = "Fake.Package1", originalRange = "9.0.1", normalizedRange = "[9.0.1, )" },
                new { id = "Fake.Package2", originalRange = "[9.0.1]", normalizedRange = "[9.0.1, 9.0.1]" },
                new { id = "Fake.Package3", originalRange = "9.0.*", normalizedRange = "[9.0.*, )" }, // TODO: Really? Was expecting something like [9.0, 9.1)
                new { id = "Fake.Package4", originalRange = "[9,10)", normalizedRange = "[9.0.0, 10.0.0)" },
            };

            foreach (var expected in expectedPackages) {
                var package = packages.Single (p => PackageIdComparer.Equals (p.Identity.Id, expected.id));
                package.SupportedVersionRange.OriginalString.ShouldEqual (expected.originalRange);
                package.SupportedVersionRange.ToNormalizedString ().ShouldEqual (expected.normalizedRange);
            }
        }

        [Test]
        public void TestWritingNewPackages ()
        {
            var page = new WorkbookPage (new WorkbookPackage ());
            var packageVersion = "9.0.1";
            var package = new InteractivePackage ("Fake.Package1", VersionRange.Parse (packageVersion));
            page.Manifest.Packages = page.Manifest.Packages.Add (package);

            var writer = new StringWriter ();
            page.Write (writer, null);
            var serializedPage = writer.ToString ();

            var deserializedPage = new WorkbookPage (new WorkbookPackage ());
            deserializedPage.Read (new StringReader (serializedPage));

            deserializedPage.Packages.Length.ShouldEqual (1);

            var deserializedPackage = deserializedPage.Packages [0];
            deserializedPackage.Identity.Id.ShouldEqual (package.Identity.Id);
            // Make sure this doesn't serialize as [9.0.1, )
            deserializedPackage.SupportedVersionRange.OriginalString.ShouldEqual (packageVersion);
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