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
            var installedPackages = await CreatePackageManager (FrameworkNames.Xamarin_iOS_1_0)
                .RestoreAsync (PackageReferenceList.Create (
                    ("Xamarin.Forms.Dynamic", "0.1.18-pre")));

            Assert.InRange (installedPackages.Count, 3, int.MaxValue);

            Assert.Collection (
                installedPackages.Where (p => p.Identity.Id == "Xamarin.Forms.Dynamic"),
                package => Assert.Equal (
                    new NuGetVersion (0, 1, 18, "pre"),
                    package.Identity.Version));

            Assert.Collection (
                installedPackages.Where (p => p.Identity.Id == InteractivePackageManager.FixedXamarinFormsPackageIdentity.Id),
                package => Assert.Equal (
                    InteractivePackageManager.FixedXamarinFormsPackageIdentity.Version,
                    package.Identity.Version));

            Assert.Collection (
                installedPackages.Where (p => p.Identity.Id == "Newtonsoft.Json"),
                package => Assert.InRange (
                    package.Identity.Version,
                    new NuGetVersion (7, 0, 1, string.Empty),
                    maxNuGetVersion));
        }

        [Fact]
        public async Task IntegrationPackageIsNotInstalled ()
        {
            var installedPackages = await CreatePackageManager (FrameworkNames.Net_4_6_1)
                .RestoreAsync (PackageReferenceList.Create (
                    (InteractivePackageManager.IntegrationPackageId, "1.0.0-rc5")));

            Assert.Empty (installedPackages.Where (
                p => p.Identity.Id == InteractivePackageManager.IntegrationPackageId));
        }

        [Fact]
        public async Task TestRoslynRestore ()
        {
            var packageReferences = PackageReferenceList.Create (
                ("Microsoft.CodeAnalysis.CSharp.Workspaces", "2.1.*"),
                ("Microsoft.CodeAnalysis.Common", "[2.*,)"));

            var installedPackages = await CreatePackageManager (FrameworkNames.Net_4_6_1)
                .RestoreAsync (packageReferences);

            var mccWorkspacesPackage = installedPackages.Single (
                p => p.Identity.Id == packageReferences [0].PackageId);
            Assert.NotEqual (
                packageReferences [0].VersionRange,
                mccWorkspacesPackage.Identity.Version.OriginalVersion);
            Assert.Equal (
                packageReferences [0].VersionRange,
                mccWorkspacesPackage.PackageReference.VersionRange);
            Assert.NotEmpty (mccWorkspacesPackage.AssemblyReferences);

            var mcCommonPackage = installedPackages.Single (
                p => p.Identity.Id == packageReferences [1].PackageId);
            Assert.NotEqual (
                packageReferences [1].VersionRange,
                mcCommonPackage.Identity.Version.OriginalVersion);
            Assert.Equal (
                packageReferences [1].VersionRange,
                mcCommonPackage.PackageReference.VersionRange);
            Assert.NotEmpty (mcCommonPackage.AssemblyReferences);

            // Demonstrate that assemblies are still gathered for non-explicit dependencies
            var mcCsharpPackage = installedPackages.Single (
                p => p.Identity.Id == "Microsoft.CodeAnalysis.CSharp");
            Assert.NotEmpty (mcCsharpPackage.AssemblyReferences);

            Assert.Equal (48, installedPackages.Count);
        }

        [Theory]
        [MemberData (nameof (GetNuGetTestCases))]
        public async Task CanInstall (FrameworkName targetFramework, PackageInstallData data)
        {
            var installedPackages = await CreatePackageManager (targetFramework)
                .RestoreAsync (PackageReferenceList.Create (
                    (data.Id, data.VersionToInstall.ToFullString ())));
            Assert.Collection (
                installedPackages.Where (
                    p => PackageIdComparer.Equals (p.Identity.Id, data.Id)),
                package => {
                    Assert.Equal (
                        data.ExpectedInstalled,
                        package.Identity.Version);
                    Assert.Equal (
                        new VersionRange (data.VersionToInstall),
                        VersionRange.Parse (package.PackageReference.VersionRange));
                });
        }
    }
}