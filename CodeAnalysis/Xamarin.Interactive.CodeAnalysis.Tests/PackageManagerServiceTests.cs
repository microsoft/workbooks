// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using NuGet.Versioning;

using Xunit;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.NuGet
{
    public class PackageManagerServiceTests
    {
        struct Services
        {
            public TestWorkspaceService WorkspaceService { get; }
            public PackageManagerService PackageManagerService { get; }
            public EvaluationService EvaluationService { get; }

            public Services (
                TestWorkspaceService workspaceService,
                PackageManagerService packageManagerService,
                EvaluationService evaluationService)
            {
                WorkspaceService = workspaceService;
                PackageManagerService = packageManagerService;
                EvaluationService = evaluationService;
            }

            public void Deconstruct (
                out TestWorkspaceService workspaceService,
                out PackageManagerService packageManagerService,
                out EvaluationService evaluationService)
            {
                workspaceService = WorkspaceService;
                packageManagerService = PackageManagerService;
                evaluationService = EvaluationService;
            }
        }

        static Task<Services> CreateServices (
            params (string id, string version) [] initialPackages)
            => CreateServices (null, null, initialPackages);

        static async Task<Services> CreateServices (
            string runtimeIdentifier = null,
            string targetFramework = null,
            (string id, string version) [] initialPackages = null)
        {
            if (runtimeIdentifier == null)
                runtimeIdentifier = Runtime.CurrentProcessRuntime.RuntimeIdentifier;

            if (targetFramework == null)
                targetFramework = ".NETCoreApp,Version=2.0";

            var workspaceService = new TestWorkspaceService (null);
            var packageManagerService = new PackageManagerService ();
            var evaluationService = new EvaluationService (
                workspaceService,
                packageManagerService,
                new EvaluationEnvironment ());

            await packageManagerService
                .InitializeAsync (
                    runtimeIdentifier,
                    new FrameworkName (targetFramework),
                    new FilePath (typeof (PackageManagerServiceTests).Assembly.Location)
                        .ParentDirectory
                        .Combine (nameof (PackageManagerServiceTests)),
                    initialPackages?.Select (
                        p => new InteractivePackageDescription (p.id, p.version)))
                .ConfigureAwait (false);

            return new Services (
                workspaceService,
                packageManagerService,
                evaluationService);
        }

        [Fact]
        public async Task InitializeEmpty ()
        {
            var (_, service, _) = await CreateServices ();
            Assert.Empty (service.PackageReferences);
        }

        [Fact]
        public async Task AspNetCoreMvc ()
        {
            var (workspace, packageManager, _) = await CreateServices (
                ("Microsoft.AspNetCore", "2.0.3"),
                ("Microsoft.AspNetCore.Mvc", "2.0.4"));

            Assert.Collection (
                packageManager.PackageReferences,
                package => Assert.Equal ("Microsoft.AspNetCore", package.PackageId),
                package => Assert.Equal ("Microsoft.AspNetCore.Mvc", package.PackageId));

            Assert.True (
                workspace.PackageAssemblyReferences.Count > 100,
                "Not enough assembly references");
        }

        static string NormalizePath (string path)
            => path
                ?.Replace ('\\', Path.DirectorySeparatorChar)
                ?.Replace ('/', Path.DirectorySeparatorChar);

        [Theory]
        [InlineData (".NETFramework,Version=2.0", "lib/net20/Newtonsoft.Json.dll")]
        [InlineData (".NETFramework,Version=3.5", "lib/net35/Newtonsoft.Json.dll")]
        [InlineData (".NETFramework,Version=4.0", "lib/net40/Newtonsoft.Json.dll")]
        [InlineData (".NETFramework,Version=4.5", "lib/net45/Newtonsoft.Json.dll")]
        [InlineData (".NETFramework,Version=4.6", "lib/net45/Newtonsoft.Json.dll")]
        [InlineData (".NETFramework,Version=4.6.1", "lib/net45/Newtonsoft.Json.dll")]
        [InlineData (".NETFramework,Version=4.7.2", "lib/net45/Newtonsoft.Json.dll")]
        [InlineData (".NETPortable,Version=v0.0,Profile=Profile328", "lib/portable-net40+sl5+win8+wp8+wpa81/Newtonsoft.Json.dll")]
        [InlineData (".NETPortable,Version=v0.0,Profile=Profile259", "lib/portable-net45+win8+wp8+wpa81/Newtonsoft.Json.dll")]
        [InlineData (".NETCoreApp,Version=2.0", "lib/netstandard2.0/Newtonsoft.Json.dll")]
        public async Task VariousTargetFrameworksAndRids (string targetFramework, string expectedAssemblyPath)
        {
            var (workspace, packageManager, _) = await CreateServices (
                null,
                targetFramework,
                new [] { ("Newtonsoft.Json", "11.0.2") });

            Assert.Collection (
                packageManager.PackageReferences,
                package => {
                    Assert.Equal ("11.0.2", package.VersionRange);
                    Assert.Equal ("Newtonsoft.Json", package.PackageId);
                });

            Assert.Collection (
                workspace.PackageAssemblyReferences,
                assemblyReference => Assert.EndsWith (
                    NormalizePath (expectedAssemblyPath),
                    NormalizePath (assemblyReference),
                    StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task TestMultiplePackageReferencesOverTime ()
        {
            // note: this test specifically tests only some well known netstandard2.0 packages
            // to simplify assembly reference assertions since no facades will be in the list

            var (workspace, packageManager, _) = await CreateServices (
                null,
                ".NETCoreApp,Version=2.0");

            await packageManager.InstallAsync (("Newtonsoft.Json", "11.0.2"));

            void AssertNewtonsoftJson (InteractivePackageDescription package)
            {
                Assert.Equal ("Newtonsoft.Json", package.PackageId);
                Assert.Equal ("11.0.2", package.VersionRange);
            }

            Assert.Collection (
                packageManager.PackageReferences,
                AssertNewtonsoftJson);

            Assert.Collection (
                workspace.PackageAssemblyReferences,
                assemblyReference => Assert.Equal ("Newtonsoft.Json.dll", assemblyReference.Name));

            await packageManager.InstallAsync (("SkiaSharp", "1.60.1"));

            void AssertSkiaSharp (InteractivePackageDescription package)
            {
                Assert.Equal ("SkiaSharp", package.PackageId);
                Assert.Equal ("1.60.1", package.VersionRange);
            }

            Assert.Collection (
                packageManager.PackageReferences,
                AssertNewtonsoftJson,
                AssertSkiaSharp);

            Assert.Collection (
                workspace.PackageAssemblyReferences,
                assemblyReference => Assert.Equal ("Newtonsoft.Json.dll", assemblyReference.Name),
                assemblyReference => Assert.Equal ("SkiaSharp.dll", assemblyReference.Name));

            await packageManager.InstallAsync (("RestSharp", "106.2.2"));

            void AssertRestSharp (InteractivePackageDescription package)
            {
                Assert.Equal ("RestSharp", package.PackageId);
                Assert.Equal ("106.2.2", package.VersionRange);
            }

            Assert.Collection (
                packageManager.PackageReferences,
                AssertNewtonsoftJson,
                AssertSkiaSharp,
                AssertRestSharp);

            Assert.Collection (
                workspace.PackageAssemblyReferences,
                assemblyReference => Assert.Equal ("Newtonsoft.Json.dll", assemblyReference.Name),
                assemblyReference => Assert.Equal ("RestSharp.dll", assemblyReference.Name),
                assemblyReference => Assert.Equal ("SkiaSharp.dll", assemblyReference.Name));
        }

        [Fact]
        public async Task TestVersionRange ()
        {
            var (_, packageManager, _) = await CreateServices (
                null,
                ".NETFramework,Version=4.5");

            await packageManager.InstallAsync (("YamlDotNet", "3.*"));

            Assert.Collection (
                packageManager.PackageReferences,
                yamlDotNet => {
                    Assert.Equal ("YamlDotNet", yamlDotNet.PackageId);
                    Assert.Equal ("3.*", yamlDotNet.VersionRange);
                });

            Assert.Collection (
                packageManager.InstalledPackages,
                yamlDotNet => {
                    Assert.Equal ("YamlDotNet", yamlDotNet.Identity.Id);
                    Assert.Equal (NuGetVersion.Parse ("3.9.0"), yamlDotNet.Identity.Version);
                    Assert.Equal ("3.*", yamlDotNet.PackageReference.VersionRange);
                });
        }
    }
}