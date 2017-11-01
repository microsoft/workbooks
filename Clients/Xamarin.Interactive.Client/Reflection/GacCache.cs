//
// GacCache.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Compilation;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Reflection
{
    static class GacCache
    {
        public static Task InitializingTask { get; private set; } = Task.CompletedTask;

        static readonly IDictionary<FilePath, ImmutableArray<FilePath>> CachedPaths
            = new Dictionary<FilePath, ImmutableArray<FilePath>> ();

        static readonly IDictionary<FilePath, AssemblyName> CachedAssemblyNames
            = new Dictionary<FilePath, AssemblyName> ();

        static readonly IDictionary<AssemblyName, ResolvedAssembly> CachedResolvedAssemblies
            = new Dictionary<AssemblyName, ResolvedAssembly> (ResolvedAssembly.NameEqualityComparer.Default);

        static readonly string Dotnet4GacPath = Path.Combine (
            Environment.GetFolderPath (Environment.SpecialFolder.Windows),
            "Microsoft.NET",
            "assembly");

        static readonly string [] WindowsGacPaths = {
            Path.Combine (Dotnet4GacPath, "GAC_MSIL"),
            Path.Combine (Dotnet4GacPath, "GAC_32"),
            Path.Combine (Dotnet4GacPath, "GAC_64")
        };

        static readonly string[] MacGacPaths = {
            "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/",
        };

        public static IReadOnlyList<string> GacPaths => InteractiveInstallation.Default.IsMac
            ? MacGacPaths
            : WindowsGacPaths;

        public static bool TryGetCachedFilePaths (FilePath key, out ImmutableArray<FilePath> files)
            => CachedPaths.TryGetValue (key, out files);

        public static bool TryGetCachedAssemblyName (FilePath key, out AssemblyName assemblyName)
            => CachedAssemblyNames.TryGetValue (key, out assemblyName);

        public static bool TryGetCachedResolvedAssembly (AssemblyName assemblyName,
            out ResolvedAssembly resolvedAssembly)
            => CachedResolvedAssemblies.TryGetValue (assemblyName, out resolvedAssembly);

        public static void Initialize ()
        {
            InitializingTask = Task.Run (() => {
                var gacPaths = GacPaths;

                // AgentType.Unknown will result in the GAC cache not being used.
                var dependencyResolver = new InteractiveDependencyResolver (AgentType.Unknown);

                foreach (var gacPath in gacPaths)
                    ProcessGacPath (gacPath, dependencyResolver);
            });
        }

        static void ProcessGacPath (string gacPath, DependencyResolver dependencyResolver)
        {
            var files = ImmutableArray<FilePath>.Empty;
            try {
                files = DependencyResolver.EnumerateAssembliesInDirectory (
                    gacPath,
                    scanRecursively: true,
                    cancellationToken: CancellationToken.None);
                CachedPaths [gacPath] = files;
            } catch (Exception e) {
                Log.Error (nameof (GacCache), "Exception when enumerating files in " +
                    $"'{gacPath}'", e);
            }

            foreach (var file in files)
                ProcessGacFile (file, dependencyResolver);
        }

        static void ProcessGacFile (FilePath file, DependencyResolver dependencyResolver)
        {
            AssemblyName assemblyName = null;
            try {
                using (var stream = File.OpenRead (file))
                using (var peReader = new PEReader (stream)) {
                    var reader = peReader.GetMetadataReader ();
                    assemblyName = reader
                        .GetAssemblyDefinition ()
                        .ReadAssemblyName (reader);
                }
            } catch { }
            CachedAssemblyNames [file] = assemblyName;

            if (assemblyName == null)
                return;

            try {
                var resolvedAssemblies = dependencyResolver.Resolve (
                    new [] { file },
                    resolveOptions: ResolveOperationOptions.ResolveReferences | ResolveOperationOptions.SkipGacCache);
                foreach (var assembly in resolvedAssemblies)
                    CachedResolvedAssemblies [assembly.AssemblyName] = assembly;
            } catch { }
        }
    }
}
