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

using Xunit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.NuGet
{
    public sealed class InteractivePackageManagerTests
    {
        static readonly NuGetVersion maxNuGetVersion = new NuGetVersion (
            int.MaxValue, int.MaxValue, int.MaxValue, string.Empty);

        public struct PackageInstallData
        {
            public string Id { get; }
            public NuGetVersion VersionToInstall { get; }
            public NuGetVersion ExpectedInstalled { get; }

            public PackageInstallData (
                string id,
                NuGetVersion versionToInstall,
                NuGetVersion expectedInstalled)
            {
                Id = id;
                VersionToInstall = versionToInstall;
                ExpectedInstalled = expectedInstalled;
            }

            public override string ToString ()
                => $" Id: {Id}, Requested: {VersionToInstall}, Expecting: {ExpectedInstalled}";
        }

        static IEnumerable<FrameworkName> Frameworks => new [] {
            FrameworkNames.Xamarin_iOS_1_0,
            FrameworkNames.Xamarin_Mac_2_0,
            FrameworkNames.MonoAndroid_5_0,
            FrameworkNames.Net_4_6_1,
        };

        internal static InteractivePackageManager CreatePackageManager (FrameworkName targetFramework)
        {
            var rootPath = new DotNetFileSystem ().GetTempDirectory ("tests");
            var localRepositoryDirectory = rootPath.Combine ("NuGet", "package-cache");

            try {
                Directory.Delete (localRepositoryDirectory, recursive: true);
            } catch (DirectoryNotFoundException) {
            }

            return new InteractivePackageManager (
                null,
                targetFramework,
                localRepositoryDirectory);
        }

        static readonly List<PackageInstallData> packagesToTestInstall
            = new List<PackageInstallData> {
            new PackageInstallData (
                "Newtonsoft.Json",
                new NuGetVersion (10, 0, 2, 0),
                new NuGetVersion (10, 0, 2, 0)
            ),
            new PackageInstallData (
                "Xamarin.Forms",
                InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version,
                InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version
            ),
            new PackageInstallData (
                "Newtonsoft.Json",
                new NuGetVersion (8, 0, 2, 0),
                new NuGetVersion (8, 0, 2, 0)
            ),
            new PackageInstallData (
                "Xamarin.Forms",
                new NuGetVersion (2, 3, 3, 180),
                InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version
            ),
            new PackageInstallData (
                "Xamarin.Forms",
                new NuGetVersion (2, 5, 0, 19271, "pre2", string.Empty),
                InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version
            ),
            new PackageInstallData (
                "semver",
                new NuGetVersion (2, 0, 4),
                new NuGetVersion (2, 0, 4)
            )
        };

        public static IEnumerable<object []> GetNuGetTestCases ()
        {
            foreach (var framework in Frameworks) {
                foreach (var package in packagesToTestInstall)
                    yield return new object [] { framework, package };
            }
        }

        [Fact]
        public async Task BadXamarinFormsDependencyIsRewritten ()
        {
            var project = CreatePackageManager (FrameworkNames.Xamarin_iOS_1_0);
            await project.InstallAsync (
                new InteractivePackage (new PackageIdentity (
                    "Xamarin.Forms.Dynamic",
                    new NuGetVersion (0, 1, 18, "pre"))),
                cancellationToken: CancellationToken.None);

            Assert.InRange (project.InstalledPackages.Count (), 3, int.MaxValue);

            Assert.Collection (
                project.InstalledPackages.Where (p => p.Identity.Id == "Xamarin.Forms.Dynamic"),
                package => Assert.Equal (
                    new NuGetVersion (0, 1, 18, "pre"),
                    package.Identity.Version));

            Assert.Collection (
                project
                    .InstalledPackages
                    .Where (p => p.Identity.Id == InteractivePackageManager.FixedXamarinFormsPackageIdentity.Id),
                package => Assert.Equal (
                    InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version,
                    package.Identity.Version));

            Assert.Collection (
                project.InstalledPackages.Where (p => p.Identity.Id == "Newtonsoft.Json"),
                package => Assert.InRange (
                    package.Identity.Version,
                    new NuGetVersion (7, 0, 1, string.Empty),
                    maxNuGetVersion));
        }

        [Fact]
        public async Task IntegrationPackageIsNotInstalled ()
        {
            var project = CreatePackageManager (FrameworkNames.Net_4_6_1);
            await project.InstallAsync (
                new InteractivePackage (new PackageIdentity (
                    InteractivePackageManager.IntegrationPackageId,
                    new NuGetVersion (1, 0, 0, string.Empty))),
                cancellationToken: CancellationToken.None);

            Assert.Empty (project.InstalledPackages.Where (
                p => p.Identity.Id == InteractivePackageManager.IntegrationPackageId));
        }

        [Fact]
        public async Task TestRoslynRestore ()
        {
            var project = CreatePackageManager (FrameworkNames.Net_4_6_1);

            var explicitPackages = new [] {
                new InteractivePackage (
                    "Microsoft.CodeAnalysis.CSharp.Workspaces",
                    VersionRange.Parse ("2.1.*")),
                new InteractivePackage (
                    "Microsoft.CodeAnalysis.Common",
                    VersionRange.Parse ("[2.*,)")),
            };

            await project.RestoreAsync (explicitPackages, CancellationToken.None);

            var mccWorkspacesPackage = project.InstalledPackages.Single (
                p => p.Identity.Id == explicitPackages [0].Identity.Id);
            Assert.NotEqual (
                explicitPackages [0],
                mccWorkspacesPackage);
            Assert.Equal (
                explicitPackages [0].SupportedVersionRange.OriginalString,
                mccWorkspacesPackage.SupportedVersionRange.OriginalString);
            Assert.True (mccWorkspacesPackage.IsExplicit);
            Assert.NotEmpty (mccWorkspacesPackage.AssemblyReferences);

            var mcCommonPackage = project.InstalledPackages.Single (
                p => p.Identity.Id == explicitPackages [1].Identity.Id);
            Assert.NotEqual (
                explicitPackages [1],
                mcCommonPackage);
            Assert.Equal (
                explicitPackages [1].SupportedVersionRange.OriginalString,
                mcCommonPackage.SupportedVersionRange.OriginalString);
            Assert.True (mcCommonPackage.IsExplicit);
            Assert.NotEmpty (mcCommonPackage.AssemblyReferences);

            // Demonstrate that assemblies are still gathered for non-explicit dependencies
            var mcCsharpPackage = project.InstalledPackages.Single (
                p => p.Identity.Id == "Microsoft.CodeAnalysis.CSharp");
            Assert.False (mcCsharpPackage.IsExplicit);
            Assert.NotEmpty (mcCsharpPackage.AssemblyReferences);

            Assert.Equal (48, project.InstalledPackages.Length);

            Assert.True (project
                .InstalledPackages
                .Where (p => p != mccWorkspacesPackage && p != mcCommonPackage)
                .All (p => p.IsExplicit == false));
        }

        [Theory]
        [MemberData (nameof (GetNuGetTestCases))]
        public async Task CanInstall (FrameworkName targetFramework, PackageInstallData data)
        {
            var project = CreatePackageManager (targetFramework);
            await project.InstallAsync (
                new InteractivePackage (new PackageIdentity (data.Id, data.VersionToInstall)),
                cancellationToken: CancellationToken.None);

            Assert.Collection (
                project.InstalledPackages.Where (
                    p => PackageIdComparer.Equals (p.Identity.Id, data.Id)),
                package => {
                    Assert.True (package.IsExplicit);
                    Assert.Equal (data.ExpectedInstalled, package.Identity.Version);
                    Assert.Equal (new VersionRange (data.ExpectedInstalled), package.SupportedVersionRange);
                });
        }
    }
}