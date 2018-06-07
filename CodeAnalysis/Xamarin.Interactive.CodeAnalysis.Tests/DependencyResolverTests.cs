//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using NuGet.Versioning;

using Xunit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
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

        static TargetCompilationConfiguration iosConfiguration = TargetCompilationConfiguration
            .CreateInitialForCompilationWorkspace ()
            .With (sdk: new Sdk (
                SdkId.XamarinIos,
                FrameworkNames.Xamarin_iOS_1_0,
                Array.Empty<string> ()));

        static ImmutableArray<ResolvedAssembly> Resolve (
            TargetCompilationConfiguration configuration,
            IReadOnlyList<InteractivePackage> installedPackages,
            FilePath frameworkPath)
        {
            var result = ImmutableArray<ResolvedAssembly>.Empty;
            var stopwatch = new System.Diagnostics.Stopwatch ();
            stopwatch.Start ();
            try {
                return result = new NativeDependencyResolver (configuration)
                    .AddAssemblySearchPath (frameworkPath.Combine ("Facades"))
                    .AddAssemblySearchPath (frameworkPath)
                    .Resolve (installedPackages.SelectMany (p => p.AssemblyReferences));
            } finally {
                stopwatch.Stop ();
                Console.WriteLine ("DependencyResolver.Resolve: {0} in {1}s",
                    result.Length, stopwatch.Elapsed.TotalSeconds);
            }
        }

        [Fact]
        public async Task TestNativeLibraries ()
        {
            var installedPackages = await InteractivePackageManagerTests
                .CreatePackageManager (TargetFrameworks.Xamarin_iOS_1_0)
                .RestoreAsync (PackageReferenceList.Create (
                    ("UrhoSharp", "1.0.410"),
                    ("SkiaSharp", "1.49.1.0")));

            var resolvedAssemblies = Resolve (
                iosConfiguration,
                installedPackages,
                TargetFrameworks.Xamarin_iOS_1_0_FrameworkPath);

            Assert.NotEmpty (resolvedAssemblies);

            Assert.Equal (
                "Urho.framework",
                resolvedAssemblies
                    .First (a => a.AssemblyName.Name == "Urho")
                    .ExternalDependencies
                    .OfType<NativeDependency> ()
                    .First ()
                    .Name);

            Assert.Equal (
                "libskia_ios.framework",
                resolvedAssemblies
                    .First (a => a.AssemblyName.Name == "SkiaSharp")
                    .ExternalDependencies
                    .OfType<NativeDependency> ()
                    .First ()
                    .Name);
        }

        [Fact]
        public async Task TestPackageResolution ()
        {
            var installedPackages = await InteractivePackageManagerTests
                .CreatePackageManager (TargetFrameworks.Xamarin_iOS_1_0)
                .RestoreAsync (PackageReferenceList.Create (
                    ("akavache", "4.1.2"),
                    ("Microsoft.Azure.Mobile.Client", "2.0.1"),
                    ("Newtonsoft.Json", "8.0.3")));

            // Ensure the specified Newtonsoft.Json is the one that gets installed
            var installedNewtonsoftJson = installedPackages.Where (p => p.Identity.Id == "Newtonsoft.Json");
            Assert.Collection (
                installedNewtonsoftJson,
                package => {
                    Assert.Equal (new NuGetVersion ("8.0.3"), package.Identity.Version);
                    Assert.Equal (new Version (8, 0, 3, 0), package.Identity.Version.Version);
                });

            var resolvedAssemblies = Resolve (
                iosConfiguration,
                installedPackages,
                TargetFrameworks.Xamarin_iOS_1_0_FrameworkPath);

            Assert.NotEmpty (resolvedAssemblies);

            // Ensure the specified Newtonsoft.Json is the one that gets resolved
            var newtonsoftJson = resolvedAssemblies.Where (r => r.AssemblyName.Name == "Newtonsoft.Json");
            Assert.Collection (
                newtonsoftJson,
                assembly => {
                    Assert.Equal (new Version (8, 0, 0, 0), assembly.AssemblyName.Version);
                    Assert.Equal (
                        "8.0.3",
                        assembly
                            .Path
                            .ParentDirectory // $profile
                            .ParentDirectory // 'lib'
                            .ParentDirectory // $nupkg_root
                            .Name);
                });
        }

        [Fact]
        public void ResolveByAbsolutePath ()
        {
            FilePath path = typeof (DependencyResolver).Assembly.Location;
            Assert.NotEmpty (new DependencyResolver ().Resolve (new [] { path }));
        }

        [Fact]
        public void ResolveByRelativePath ()
        {
            FilePath path = typeof (DependencyResolver).Assembly.Location;
            Assert.NotEmpty (new DependencyResolver { BaseDirectory = path.ParentDirectory }
                .Resolve (new [] {
                    new FilePath ("..").Combine (
                        path.ParentDirectory.Name,
                        (FilePath)path.Name)
                }));
        }

        [Fact]
        public void ResolveByFileNameOnly ()
        {
            FilePath path = typeof (DependencyResolver).Assembly.Location;

            Assert.NotEmpty (new DependencyResolver ()
                .AddAssemblySearchPath (path.ParentDirectory)
                .Resolve (new [] { (FilePath)path.Name }));

            Assert.NotEmpty (new DependencyResolver { BaseDirectory = path.ParentDirectory }
                .Resolve (new [] { (FilePath)path.Name }));
        }
    }
}