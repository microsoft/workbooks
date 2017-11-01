//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Reflection;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public class DependencyResolverTests
    {
        static class TargetFrameworks
        {
            public static readonly FrameworkName Xamarin_iOS_1_0
                = new FrameworkName ("Xamarin.iOS", new Version (1, 0));

            public static readonly FilePath Xamarin_iOS_1_0_FrameworkPath =
                "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.iOS";

            public static readonly FrameworkName Net_4_5
                = new FrameworkName (".NETFramework", new Version (4, 5));
        }

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

        static ImmutableArray<ResolvedAssembly> Resolve (
            AgentType agentType,
            InteractivePackageManager project,
            FilePath frameworkPath)
        {
            var result = ImmutableArray<ResolvedAssembly>.Empty;
            var stopwatch = new System.Diagnostics.Stopwatch ();
            stopwatch.Start ();
            try {
                return result = new NativeDependencyResolver (agentType)
                    .AddAssemblySearchPath (frameworkPath.Combine ("Facades"))
                    .AddAssemblySearchPath (frameworkPath)
                    .Resolve (project.InstalledPackages.SelectMany (
                        p => p.AssemblyReferences));
            } finally {
                stopwatch.Stop ();
                Console.WriteLine ("DependencyResolver.Resolve: {0} in {1}s",
                    result.Length, stopwatch.Elapsed.TotalSeconds);
            }
        }

        [Test]
        public async Task TestNativeLibraries ()
        {
            var project = CreatePackageManager (TargetFrameworks.Xamarin_iOS_1_0);
            await project.InstallPackageAsync (
                new InteractivePackage (new PackageIdentity ("UrhoSharp", new NuGetVersion ("1.0.410"))),
                sourceRepository: null, // use default
                cancellationToken: CancellationToken.None);
            await project.InstallPackageAsync (
                new InteractivePackage (new PackageIdentity ("SkiaSharp", new NuGetVersion ("1.49.1.0"))),
                sourceRepository: null, // use default
                cancellationToken: CancellationToken.None);

            var resolvedAssemblies = Resolve (
                AgentType.iOS,
                project,
                TargetFrameworks.Xamarin_iOS_1_0_FrameworkPath);

            resolvedAssemblies.Length.ShouldBeGreaterThan (0);

            resolvedAssemblies
                .First (a => a.AssemblyName.Name == "Urho")
                .ExternalDependencies
                .OfType<NativeDependency> ()
                .First ()
                .Name
                .ShouldEqual ("Urho.framework");

            resolvedAssemblies
                .First (a => a.AssemblyName.Name == "SkiaSharp")
                .ExternalDependencies
                .OfType<NativeDependency> ()
                .First ()
                .Name
                .ShouldEqual ("libskia_ios.framework");
        }

        [Test]
        public async Task TestPackageResolution ()
        {
            var project = CreatePackageManager (TargetFrameworks.Xamarin_iOS_1_0);

            var packagesToInstall = new [] {
                new InteractivePackage ("akavache", VersionRange.Parse ("4.1.2")),
                new InteractivePackage ("Microsoft.Azure.Mobile.Client", VersionRange.Parse ("2.0.1")),
                new InteractivePackage ("Newtonsoft.Json", VersionRange.Parse ("8.0.3"))
            };

            await project.RestorePackagesAsync (packagesToInstall, CancellationToken.None);

            // Ensure the specified Newtonsoft.Json is the one that gets installed
            var installedNewtonsoftJson = project.InstalledPackages.Where (p => p.Identity.Id == "Newtonsoft.Json");
            installedNewtonsoftJson.Count ().ShouldEqual (1);
            installedNewtonsoftJson
                .Single ()
                .Identity
                .Version
                .ShouldEqual (new NuGetVersion ("8.0.3"));

            var resolvedAssemblies = Resolve (
                AgentType.iOS,
                project,
                TargetFrameworks.Xamarin_iOS_1_0_FrameworkPath);

            resolvedAssemblies.Length.ShouldBeGreaterThan (0);

            // Ensure the specified Newtonsoft.Json is the one that gets resolved
            var newtonsoftJson = resolvedAssemblies.Where (r => r.AssemblyName.Name == "Newtonsoft.Json");
            newtonsoftJson.Count ().ShouldEqual (1);
            newtonsoftJson
                .Single ()
                .AssemblyName
                .Version
                .ShouldEqual (new Version (8, 0, 0, 0));
            newtonsoftJson
                .Single ()
                .Path
                .ParentDirectory // $profile
                .ParentDirectory // 'lib'
                .ParentDirectory // $nupkg_root
                .Name
                .ShouldEqual ("8.0.3");

            var newtonsoftJsonPackage = project.InstalledPackages.Where (p => p.Identity.Id == "Newtonsoft.Json");
            newtonsoftJsonPackage.Count ().ShouldEqual (1);
            newtonsoftJsonPackage
                .First ()
                .Identity
                .Version
                .Version
                .ShouldEqual (new Version (8, 0, 3, 0));
        }

        [Test]
        public void ResolveByAbsolutePath ()
        {
            FilePath path = typeof (DependencyResolver).Assembly.Location;
            new DependencyResolver ()
                .Resolve (new [] { path })
                .Length
                .ShouldBeGreaterThan (1);
        }

        [Test]
        public void ResolveByRelativePath ()
        {
            FilePath path = typeof (DependencyResolver).Assembly.Location;
            new DependencyResolver { BaseDirectory = path.ParentDirectory }
                .Resolve (new [] {
                    new FilePath ("..").Combine (
                        path.ParentDirectory.Name,
                        (FilePath)path.Name)
                })
                .Length
                .ShouldBeGreaterThan (1);
        }

        [Test]
        public void ResolveByFileNameOnly ()
        {
            FilePath path = typeof (DependencyResolver).Assembly.Location;

            new DependencyResolver ()
                .AddAssemblySearchPath (path.ParentDirectory)
                .Resolve (new [] { (FilePath)path.Name })
                .Length
                .ShouldBeGreaterThan (1);

            new DependencyResolver { BaseDirectory = path.ParentDirectory }
                .Resolve (new [] { (FilePath)path.Name })
                .Length
                .ShouldBeGreaterThan (1);
        }
    }
}