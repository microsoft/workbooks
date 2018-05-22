//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    public class NativeDependencyResolver : DependencyResolver
    {
        const string TAG = nameof (NativeDependencyResolver);

        const string skiaSharpDllMap = @"
            <configuration>
              <dllmap dll='libSkiaSharp.dylib' os='osx' target='../../runtimes/osx/native/libSkiaSharp.dylib'/>
              <dllmap dll='libSkiaSharp' os='windows' cpu='x86-64' wordsize='64' target='../../runtimes/win7-x64/native/libSkiaSharp.dll'/>
              <!-- On x86, or x86-64 w/ a 32-bit word size, pick the 32-bit libSkiaSharp. -->
              <dllmap dll='libSkiaSharp' os='windows' cpu='x86,x86-64' wordsize='32' target='../../runtimes/win7-x86/native/libSkiaSharp.dll'/>
            </configuration>
        ";

        const string urhoSharpDllMap = @"
            <configuration>
              <dllmap dll='mono-urho' os='osx' target='../../native/Mac/libmono-urho.dylib'/>
              <dllmap dll='mono-urho' os='windows' cpu='x86-64' wordsize='64' target='../../native/Win64_DirectX/mono-urho.dll'/>
              <!-- On x86, or x86-64 w/ a 32-bit word size, pick the 32-bit libSkiaSharp. -->
              <dllmap dll='mono-urho' os='windows' cpu='x86,x86-64' wordsize='32' target='../../native/Win32_DirectX/mono-urho.dll'/>
            </configuration>
        ";

        const string libuvDllMap = @"
            <configuration>
                <dllmap dll='libuv' os='osx' target='{0}/runtimes/osx/native/libuv.dylib'/>
                <dllmap dll='libuv' os='windows' cpu='x86-64' wordsize='64' target='{0}/runtimes/win-x64/native/libuv.dll'/>
                <dllmap dll='libuv' os='windows' cpu='x86,x86-64' wordsize='32' target='{0}/runtimes/win-x86/native/libuv.dll'/>
            </configuration>
        ";

        readonly Architecture agentArchitecture;

        internal TargetCompilationConfiguration CompilationConfiguration { get; }

        internal NativeDependencyResolver (TargetCompilationConfiguration compilationConfiguration)
        {
            CompilationConfiguration = compilationConfiguration;

            // Agents always send an architecture value via the TCC.
            agentArchitecture = compilationConfiguration.Runtime.Architecture.Value;
        }

        protected override ResolvedAssembly ParseAssembly (
            ResolveOperation resolveOperation,
            FilePath path,
            PEReader peReader,
            MetadataReader metadataReader)
        {
            var resolvedAssembly = base.ParseAssembly (resolveOperation, path, peReader, metadataReader);
            var nativeDependencies = new List<ExternalDependency> ();

            if (CompilationConfiguration.Sdk.Is (SdkId.XamarinIos))
                nativeDependencies.AddRange (
                    GetEmbeddedFrameworks (
                        resolveOperation,
                        path,
                        peReader,
                        metadataReader));


            var dllMap = new DllMap (CompilationConfiguration.Runtime);

            // Is there a config file next to the assembly? Use that.
            // TODO: Check user prefs path or something so that users can write DllMaps
            var configPath = path + ".config";
            if (File.Exists (configPath))
                dllMap.Load (configPath);
            else {
                switch (path.Name) {
                case "SkiaSharp.dll":
                    dllMap.LoadXml (skiaSharpDllMap);
                    break;
                case "UrhoSharp.dll":
                    dllMap.LoadXml (urhoSharpDllMap);
                    break;
                case "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.dll":
                    dllMap.LoadXml (GetKestrelDllMap (path));
                    break;
                }
            }

            nativeDependencies.AddRange (
                dllMap.Select (mapping =>
                    new NativeDependency (
                        mapping.Key.LibraryName,
                        path.ParentDirectory.Combine(mapping.Value.LibraryName).FullPath)));

            return resolvedAssembly.WithExternalDependencies (nativeDependencies);
        }

        string GetKestrelDllMap (FilePath path)
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
            return string.Format (libuvDllMap, libuvPackagePath);
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