//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

using Should;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public class NuGetTests
    {
        public class PackageInstallData
        {
            public string Id;
            public NuGetVersion VersionToInstall;
            public NuGetVersion ExpectedInstalled;

            public override string ToString ()
                => $" Id: {Id}, Requested: {VersionToInstall}, Expecting: {ExpectedInstalled}";
        }

        static IEnumerable<FrameworkName> Frameworks => new [] {
            FrameworkNames.Xamarin_iOS_1_0,
            FrameworkNames.Xamarin_Mac_2_0,
            FrameworkNames.MonoAndroid_5_0,
            FrameworkNames.Net_4_6_1,
        };

        static InteractivePackageManager CreatePackageManager (FrameworkName targetFramework)
        {
            var rootPath = ClientApp.SharedInstance.FileSystem.GetTempDirectory ("tests");
            var localRepositoryDirectory = rootPath.Combine ("NuGet", "package-cache");

            try {
                Directory.Delete (localRepositoryDirectory, recursive: true);
            } catch (DirectoryNotFoundException) {
            }

            return new InteractivePackageManager (
                targetFramework,
                localRepositoryDirectory);
        }

        static readonly List<PackageInstallData> packagesToTestInstall = new List<PackageInstallData> {
            new PackageInstallData {
                Id = "Newtonsoft.Json",
                VersionToInstall = new NuGetVersion (10, 0, 2, 0),
                ExpectedInstalled = new NuGetVersion (10, 0, 2, 0),
            },
            new PackageInstallData {
                Id = "Xamarin.Forms",
                VersionToInstall = InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version,
                ExpectedInstalled = InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version,
            },
            new PackageInstallData {
                Id = "Newtonsoft.Json",
                VersionToInstall = new NuGetVersion (8, 0, 2, 0),
                ExpectedInstalled = new NuGetVersion (8, 0, 2, 0),
            },
            new PackageInstallData {
                Id = "Xamarin.Forms",
                VersionToInstall = new NuGetVersion (2, 3, 3, 180),
                ExpectedInstalled = InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version,
            },
            new PackageInstallData {
                Id = "Xamarin.Forms",
                VersionToInstall = new NuGetVersion (2, 5, 0, 19271, "pre2", string.Empty),
                ExpectedInstalled = InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version,
            },
            new PackageInstallData {
                Id = "semver",
                VersionToInstall = new NuGetVersion (2, 0, 4),
                ExpectedInstalled = new NuGetVersion (2, 0, 4)
            }
        };

        static IEnumerable<ITestCaseData> GetNuGetTestCases ()
        {
            foreach (var framework in Frameworks) {
                foreach (var package in packagesToTestInstall)
                    yield return new TestCaseData (framework, package);
            }
        }

        [Test]
        public async Task BadXamarinFormsDependencyIsRewritten ()
        {
            var project = CreatePackageManager (FrameworkNames.Xamarin_iOS_1_0);
            await project.InstallPackageAsync (
                new InteractivePackage (new PackageIdentity (
                    "Xamarin.Forms.Dynamic",
                    new NuGetVersion (0, 1, 18, "pre"))),
                sourceRepository: null, // use default
                cancellationToken: CancellationToken.None);

            project.InstalledPackages.Count ().ShouldBeGreaterThanOrEqualTo (3);

            var root = project.InstalledPackages.Where (p => p.Identity.Id == "Xamarin.Forms.Dynamic");
            root.Count ().ShouldEqual (1);
            root.First ().Identity.Version.ShouldEqual (new NuGetVersion (0, 1, 18, "pre"));

            var xf = project.InstalledPackages.Where (
                p => p.Identity.Id == InteractivePackageManager.FixedXamarinFormsPackageIdentity.Id);
            xf.Count ().ShouldEqual (1);
            xf.First ().Identity.Version.ShouldEqual (
                InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version);

            var njs = project.InstalledPackages.Where (p => p.Identity.Id == "Newtonsoft.Json");
            njs.Count ().ShouldEqual (1);
            njs.First ().Identity.Version.ShouldBeGreaterThanOrEqualTo (
                new NuGetVersion (7, 0, 1, string.Empty));
        }

        [Test]
        public async Task IntegrationPackageIsNotInstalled ()
        {
            var project = CreatePackageManager (FrameworkNames.Net_4_6_1);
            await project.InstallPackageAsync (
                new InteractivePackage (new PackageIdentity (
                    PackageManagerViewModel.IntegrationPackageId,
                    new NuGetVersion (1, 0, 0, string.Empty))),
                sourceRepository: null, // use default
                cancellationToken: CancellationToken.None);

            var package = project.InstalledPackages.Where (
                p => p.Identity.Id == PackageManagerViewModel.IntegrationPackageId);
            package.Count ().ShouldEqual (0);
        }

        [Test]
        public async Task TestRestore ()
        {
            var project = CreatePackageManager (FrameworkNames.Net_4_6_1);

            var explicitPackages = new [] {
                new InteractivePackage (
                    "Microsoft.CodeAnalysis.CSharp.Workspaces",
                    VersionRange.Parse ("2.1.*")),
                new InteractivePackage (
                    "Microsoft.CodeAnalysis.Common",
                    VersionRange.Parse ("[2.*,2.9)")),
            };

            await project.RestorePackagesAsync (explicitPackages, CancellationToken.None);

            var mccWorkspacesPackage = project.InstalledPackages.Single (
                p => p.Identity.Id == explicitPackages [0].Identity.Id);
            mccWorkspacesPackage.ShouldNotEqual (explicitPackages [0]);
            mccWorkspacesPackage.SupportedVersionRange.OriginalString.ShouldEqual (
                explicitPackages [0].SupportedVersionRange.OriginalString);
            mccWorkspacesPackage.IsExplicit.ShouldBeTrue ();
            mccWorkspacesPackage.AssemblyReferences.ShouldNotBeEmpty ();

            var mcCommonPackage = project.InstalledPackages.Single (
                p => p.Identity.Id == explicitPackages [1].Identity.Id);
            mcCommonPackage.ShouldNotEqual (explicitPackages [1]);
            mcCommonPackage.SupportedVersionRange.OriginalString.ShouldEqual (
                explicitPackages [1].SupportedVersionRange.OriginalString);
            mcCommonPackage.IsExplicit.ShouldBeTrue ();
            mcCommonPackage.AssemblyReferences.ShouldNotBeEmpty ();

            // Demonstrate that assemblies are still gathered for non-explicit dependencies
            var mcCsharpPackage = project.InstalledPackages.Single (
                p => p.Identity.Id == "Microsoft.CodeAnalysis.CSharp");
            mcCsharpPackage.IsExplicit.ShouldBeFalse ();
            mcCsharpPackage.AssemblyReferences.ShouldNotBeEmpty ();

            project.InstalledPackages.Length.ShouldEqual (48);

            project
                .InstalledPackages
                .Where (p => p != mccWorkspacesPackage && p != mcCommonPackage)
                .All (p => p.IsExplicit == false)
                .ShouldBeTrue ();
        }

        [TestCaseSource (nameof (GetNuGetTestCases))]
        public async Task CanInstall (FrameworkName targetFramework, PackageInstallData data)
        {
            var project = CreatePackageManager (targetFramework);
            await project.InstallPackageAsync (
                new InteractivePackage (new PackageIdentity (data.Id, data.VersionToInstall)),
                sourceRepository: null, // use default
                cancellationToken: CancellationToken.None);

            var packages = project.InstalledPackages.Where (
                p => PackageIdComparer.Equals (p.Identity.Id, data.Id));

            packages.Count ().ShouldEqual (1);

            var package = packages.Single ();
            package.IsExplicit.ShouldBeTrue ();
            package.Identity.Version.ShouldEqual (data.ExpectedInstalled);
            package.SupportedVersionRange.ShouldEqual (new VersionRange (data.ExpectedInstalled));
        }
    }
}
