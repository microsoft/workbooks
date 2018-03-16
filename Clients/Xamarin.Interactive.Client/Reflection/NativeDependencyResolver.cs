//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using NuGet.Packaging;
using NuGet.Versioning;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Reflection
{
    class NativeDependencyResolver : DependencyResolver
    {
        const string TAG = nameof (NativeDependencyResolver);

        readonly Architecture agentArchitecture;

        protected AgentType AgentType { get; }

        public NativeDependencyResolver (AgentType agentType)
        {
            AgentType = agentType;

            agentArchitecture = Environment.Is64BitProcess &&
                (HostEnvironment.OS != HostOS.Windows || AgentType != AgentType.DotNetCore)
                ? Architecture.X64
                : Architecture.X86;
        }

        protected override ResolvedAssembly ParseAssembly (
            ResolveOperation resolveOperation,
            FilePath path,
            PEReader peReader,
            MetadataReader metadataReader)
        {
            var resolvedAssembly = base.ParseAssembly (resolveOperation, path, peReader, metadataReader);
            var nativeDependencies = new List<ExternalDependency> ();

            if (AgentType == AgentType.iOS)
                nativeDependencies.AddRange (
                    GetEmbeddedFrameworks (
                        resolveOperation,
                        path,
                        peReader,
                        metadataReader));

            // HACK: Hard-code hacks for SkiaSharp.
            if (path.Name == "SkiaSharp.dll")
                nativeDependencies.AddRange (GetSkiaSharpDependencies (path));

            // HACK: Hard-code hacks for UrhoSharp.
            if (path.Name == "Urho.dll")
                nativeDependencies.AddRange (GetUrhoSharpDependencies (path));

            if (path.Name == "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.dll")
                nativeDependencies.AddRange (GetKestrelLibuvDependencies (path));

            return resolvedAssembly.WithExternalDependencies (nativeDependencies);
        }

        IEnumerable<ExternalDependency> GetKestrelLibuvDependencies (FilePath path)
        {
            var packageDirectoryPath = path.ParentDirectory.ParentDirectory.ParentDirectory;

            // Get all the available libuv's
            var allPackagesPath = packageDirectoryPath.ParentDirectory.ParentDirectory;
            var libuvPackagesPath = allPackagesPath.Combine ("libuv");
            var libuvVersions = libuvPackagesPath.EnumerateDirectories ()
                .Select (dir => NuGetVersion.Parse (dir.Name))
                .ToArray ();

            // Read the transport's nuspec to find the best matching version of libuv
            var nuspecPath = packageDirectoryPath.Combine (
                "microsoft.aspnetcore.server.kestrel.transport.libuv.nuspec");
            var nuspecData = new NuspecReader (nuspecPath);
            var libuvDependencyVersion = nuspecData.GetDependencyGroups ()
                .SelectMany (dg => dg.Packages)
                .Where (p => PackageIdComparer.Equals (p.Id, "Libuv"))
                .Distinct (PackageIdComparer.Default)
                .SingleOrDefault()
                ?.VersionRange
                .FindBestMatch (libuvVersions);

            var libuvPackagePath = libuvPackagesPath.Combine (libuvDependencyVersion.ToString ());

            var architecture = agentArchitecture.ToString ().ToLowerInvariant ();
            var isMac = false;

            switch (HostEnvironment.OS) {
            case HostOS.Windows:
                break;
            case HostOS.macOS:
                isMac = true;
                break;
            default:
                yield break;
            }

            string runtimeName, nativeLibName;
            switch (AgentType) {
            case AgentType.WPF:
                // We need the win7-<bitness> library here.
                nativeLibName = "libuv.dll";
                runtimeName = $"win-{architecture}";
                break;
            case AgentType.Console:
            case AgentType.MacNet45:
            case AgentType.MacMobile:
            case AgentType.DotNetCore:
                nativeLibName = isMac ? "libuv.dylib" : "libuv.dll";
                runtimeName = isMac ? "osx" : $"win-{architecture}";
                break;
            default:
                yield break;
            }

            var nativeLibraryPath = libuvPackagePath.Combine (
                "runtimes",
                runtimeName,
                "native",
                nativeLibName
            );

            if (nativeLibraryPath.FileExists)
                yield return new NativeDependency (nativeLibraryPath.Name, nativeLibraryPath);
        }

        IEnumerable<ExternalDependency> GetSkiaSharpDependencies (FilePath path)
        {
            var packageDirectoryPath = path.ParentDirectory.ParentDirectory.ParentDirectory;

            var architecture = agentArchitecture.ToString ().ToLowerInvariant ();
            var isMac = false;

            switch (HostEnvironment.OS) {
            case HostOS.Windows:
                break;
            case HostOS.macOS:
                isMac = true;
                break;
            default:
                yield break;
            }

            string runtimeName, nativeLibName;
            switch (AgentType) {
            case AgentType.WPF:
                // We need the win7-<bitness> library here.
                nativeLibName = "libSkiaSharp.dll";
                runtimeName = $"win7-{architecture}";
                break;
            case AgentType.Console:
            case AgentType.MacNet45:
            case AgentType.MacMobile:
            case AgentType.DotNetCore:
                nativeLibName = isMac ? "libSkiaSharp.dylib" : "libSkiaSharp.dll";
                runtimeName = isMac ? "osx" : $"win7-{architecture}";
                break;
            default:
                yield break;
            }

            var nativeLibraryPath = packageDirectoryPath.Combine (
                "runtimes",
                runtimeName,
                "native",
                nativeLibName
            );

            if (nativeLibraryPath.FileExists)
                yield return new NativeDependency (nativeLibraryPath.Name, nativeLibraryPath);
        }

        IEnumerable<ExternalDependency> GetUrhoSharpDependencies (FilePath path)
        {
            var packageDirectoryPath = path.ParentDirectory.ParentDirectory.ParentDirectory;

            var architecture = agentArchitecture == Architecture.X64 ? "64" : "32";
            var isMac = false;

            switch (HostEnvironment.OS) {
            case HostOS.Windows:
                break;
            case HostOS.macOS:
                isMac = true;
                break;
            default:
                yield break;
            }

            var nativeLibName = isMac ? "libmono-urho.dylib" : "mono-urho.dll";

            string runtimeName;
            switch (AgentType) {
            case AgentType.WPF:
                runtimeName = $"Win{architecture}";
                break;
            case AgentType.MacNet45:
            case AgentType.MacMobile:
            case AgentType.Console:
            case AgentType.DotNetCore:
                runtimeName = isMac ? "Mac" : $"Win{architecture}";
                break;
            case AgentType.Android:
                nativeLibName = "libmono-urho.so";
                yield break;
            default:
                yield break;
            }

            var nativeLibraryPath = packageDirectoryPath.Combine (
                "native",
                runtimeName,
                nativeLibName
            );

            if (nativeLibraryPath.FileExists)
                yield return new NativeDependency (nativeLibraryPath.Name, nativeLibraryPath);
        }

        IEnumerable<ExternalDependency> GetEmbeddedFrameworks (
            ResolveOperation resolveOperation,
            FilePath path,
            PEReader peReader,
            MetadataReader metadataReader)
        {
            var resourceNames = metadataReader
                .GetLinkWithLibraryNames ()
                .ToImmutableHashSet ();

            var resources = metadataReader
                .ManifestResources
                .Select (metadataReader.GetManifestResource)
                 .Where (SrmExtensions.IsExtractable);

            foreach (var resource in resources) {
                resolveOperation.CancellationToken.ThrowIfCancellationRequested ();

                var name = new FilePath (metadataReader.GetString (resource.Name));
                if (!resourceNames.Contains (name))
                    continue;

                var extractPath = new FilePath (path + ".resources").Combine (name);

                if (extractPath.Extension != ".framework") {
                    Log.Error (TAG, "Unsupported [LinkWith] embedded native resource " +
                        $"'{name}' in assembly '{path}'");
                    continue;
                }

                if (extractPath.DirectoryExists)
                    Log.Debug (TAG, "Skipping extraction of embedded manifest " +
                        $"resource '{name}' in assembly '{path}'");
                else {
                    Log.Debug (TAG, "Extracting embedded manifest resource " +
                        $"'{name}' in assembly '{path}'");

                    using (var archive = new ZipArchive (resource.GetStream (peReader)))
                        archive.Extract (
                            extractPath,
                            cancellationToken: resolveOperation.CancellationToken);
                }

                // FIXME: need to check Info.plist that CFBundlePackageType is
                // a framework (FMWK) and then read the actual framework library
                // name (CFBundleExecutable), but that means we need macdev, etc.
                var libPath = extractPath.Combine (name.NameWithoutExtension);
                if (libPath.FileExists)
                    yield return new NativeDependency (name, libPath);
                else
                    Log.Error (TAG, $"Embedded native framework '{name}' in assembly '{path}' " +
                        "has an unconventional structure and is not supported");
            }
        }
    }
}