//
// DependencyResolver.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Reflection
{
    class DependencyResolver
    {
        protected struct SearchPath
        {
            public FilePath FilePath;
            public bool ScanRecursively;
        }

        protected class ResolveOperation
        {
            readonly Stopwatch stopwatch = new Stopwatch ();

            public ResolveOperation ()
                => stopwatch.Start ();

            public ImmutableStack<SearchPath> AssemblySearchPaths;
            public ResolveOperationOptions Options;
            public ImmutableHashSet<FilePath> VisitedFiles;
            public ImmutableDictionary<FilePath, ImmutableArray<FilePath>> VisitedDirectories;
            public ImmutableDictionary<AssemblyName, ResolvedAssembly> ResolvedAssemblies;
            public ImmutableDictionary<string, AssemblyName> PathToNameMap;
            public CancellationToken CancellationToken;

            public ImmutableArray<ResolvedAssembly> GetFinalResolvedAssemblies ()
            {
                var assemblies = ResolvedAssemblies.Values.TopologicallySorted (CancellationToken);

                stopwatch.Stop ();

                if (!Options.HasFlag (ResolveOperationOptions.SkipGacCache))
                    Log.Debug (TAG, $"Resolved {assemblies.Length} assemblies in {stopwatch.Elapsed}");

                return assemblies;
            }
        }

        const string TAG = nameof (DependencyResolver);

        ImmutableArray<SearchPath> assemblySearchPaths = ImmutableArray<SearchPath>.Empty;

        protected bool UseGacCache { get; set; }

        public FilePath BaseDirectory { get; set; }

        public DependencyResolver AddAssemblySearchPath (
            FilePath searchPath,
            bool scanRecursively = false)
        {
            if (searchPath.IsNull)
                throw new ArgumentNullException (nameof (searchPath));

            if (!assemblySearchPaths.Any (sp => sp.FilePath == searchPath))
                assemblySearchPaths = assemblySearchPaths.Add (new SearchPath {
                    FilePath = searchPath,
                    ScanRecursively = scanRecursively
                });

            return this;
        }

        public DependencyResolver RemoveAssemblySearchPath (FilePath searchPath)
        {
            if (!searchPath.IsNull)
                assemblySearchPaths = assemblySearchPaths.RemoveAll (
                    sp => sp.FilePath == searchPath);

            return this;
        }

        ResolveOperation CreateResolveOperation (ResolveOperationOptions options, CancellationToken cancellationToken)
        {
            var pathStack = ImmutableStack<SearchPath>.Empty;
            foreach (var path in assemblySearchPaths)
                pathStack = pathStack.Push (path);

            // If cache use has been disabled at the DR level, ignore what's passed and make sure it is
            // not used.
            if (UseGacCache == false)
                options |= ResolveOperationOptions.SkipGacCache;

            return new ResolveOperation {
                AssemblySearchPaths = pathStack,
                Options = options,
                VisitedFiles = ImmutableHashSet<FilePath>.Empty,
                VisitedDirectories = ImmutableDictionary<FilePath, ImmutableArray<FilePath>>.Empty,
                ResolvedAssemblies = ImmutableDictionary<AssemblyName, ResolvedAssembly>
                    .Empty
                    .WithComparers (
                        ResolvedAssembly.NameEqualityComparer.Default,
                        NeverEqualComparer<ResolvedAssembly>.Default),
                PathToNameMap = ImmutableDictionary<string, AssemblyName>.Empty,
                CancellationToken = cancellationToken
            };
        }

        public ResolvedAssembly ResolveWithoutReferences (
            FilePath assemblyPath,
            CancellationToken cancellationToken = default (CancellationToken))
            => ResolveFile (CreateResolveOperation (ResolveOperationOptions.None, cancellationToken), assemblyPath);

        public ImmutableArray<ResolvedAssembly> Resolve (
            IEnumerable<FilePath> assemblyPaths,
            ResolveOperationOptions resolveOptions = ResolveOperationOptions.ResolveReferences,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            var resolveOperation = CreateResolveOperation (resolveOptions, cancellationToken);

            foreach (var toplevelAssembly in assemblyPaths.ToImmutableArray ()) {
                resolveOperation.CancellationToken.ThrowIfCancellationRequested ();
                ResolveFile (resolveOperation, toplevelAssembly);
            }

            return resolveOperation.GetFinalResolvedAssemblies ();
        }

        public ResolvedAssembly ResolveWithoutReferences (
            AssemblyName assemblyName,
            CancellationToken cancellationToken = default (CancellationToken))
            => ResolveName (CreateResolveOperation (ResolveOperationOptions.None, cancellationToken), assemblyName);

        public ImmutableArray<ResolvedAssembly> Resolve (
            IEnumerable<AssemblyName> assemblyNames,
            ResolveOperationOptions resolveOptions = ResolveOperationOptions.ResolveReferences,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            var resolveOperation = CreateResolveOperation (resolveOptions, cancellationToken);

            foreach (var assemblyName in assemblyNames) {
                resolveOperation.CancellationToken.ThrowIfCancellationRequested ();
                ResolveName (resolveOperation, assemblyName);
            }

            return resolveOperation.GetFinalResolvedAssemblies ();
        }

        public ImmutableArray<ResolvedAssembly> Resolve (
            IEnumerable<CodeAnalysis.AssemblyDefinition> assemblies,
            ResolveOperationOptions resolveOptions = ResolveOperationOptions.ResolveReferences,
            bool resolveByNameFallback = true,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            var resolveOperation = CreateResolveOperation (resolveOptions, cancellationToken);

            foreach (var assembly in assemblies) {
                resolveOperation.CancellationToken.ThrowIfCancellationRequested ();

                if (assembly.Content.Location.FileExists || !resolveByNameFallback) {
                    // Check if we've already cached this assembly.
                    ResolvedAssembly cachedAssembly;
                    if (!resolveOperation.Options.HasFlag (ResolveOperationOptions.SkipGacCache) &&
                        GacCache.TryGetCachedResolvedAssembly (assembly.Name, out cachedAssembly)) {
                        resolveOperation.ResolvedAssemblies =
                            resolveOperation.ResolvedAssemblies.SetItem (
                                assembly.Name,
                                cachedAssembly);
                        continue;
                    }

                    // We've already checked if the file exists, so don't check again.
                    ResolveFile (resolveOperation, assembly.Content.Location, checkExists: false);
                    continue;
                }

                ResolveName (resolveOperation, assembly.Name);
            }

            return resolveOperation.GetFinalResolvedAssemblies ();
        }

        ResolvedAssembly ResolveFileForName (ResolveOperation resolveOperation,
            FilePath path, AssemblyName assemblyName, bool checkExists = true)
        {
            if (!IsAssemblyFile (path, checkExists))
                return null;

            AssemblyName fileAssemblyName;

            if (!resolveOperation.PathToNameMap.TryGetValue (path, out fileAssemblyName)) {
                if (resolveOperation.Options.HasFlag (ResolveOperationOptions.SkipGacCache) ||
                    !GacCache.TryGetCachedAssemblyName (path, out fileAssemblyName)) {
                    try {
                        using (var stream = File.OpenRead (path))
                            using (var peReader = new PEReader (stream)) {
                                if (peReader.HasMetadata) {
                                    var reader = peReader.GetMetadataReader ();
                                    fileAssemblyName = reader
                                        .GetAssemblyDefinition ()
                                        .ReadAssemblyName (reader);
                                }
                            }
                    } catch {
                    }

                    resolveOperation.PathToNameMap = resolveOperation.PathToNameMap.Add (
                        path,
                        fileAssemblyName);

                    if (fileAssemblyName == null)
                        return null;
                }
            }

            if (ResolvedAssembly.NameEqualityComparer.Default.Equals (fileAssemblyName, assemblyName)) {
                ResolvedAssembly cachedResolvedAssembly;
                if (resolveOperation.Options.HasFlag (ResolveOperationOptions.SkipGacCache) ||
                    !GacCache.TryGetCachedResolvedAssembly (assemblyName, out cachedResolvedAssembly))
                    return ResolveFile (resolveOperation, path);
                return cachedResolvedAssembly;
            }

            return null;
        }

        public static ImmutableArray<FilePath> EnumerateAssembliesInDirectory (
            FilePath directory,
            bool scanRecursively,
            CancellationToken cancellationToken)
        {
            var searchOption = scanRecursively
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var builder = ImmutableArray.CreateBuilder<FilePath> (1);

            foreach (FilePath file in directory.EnumerateFiles (searchOption: searchOption)) {
                cancellationToken.ThrowIfCancellationRequested ();

                if (IsAssemblyFile (file, checkExists: false))
                    builder.Add (file);
            }

            return builder.ToImmutable ();
        }

        ResolvedAssembly ResolveFirstInDirectoryForName (ResolveOperation resolveOperation,
            SearchPath parentDirectory, AssemblyName assemblyName)
        {
            ImmutableArray<FilePath> files;

            var visitedDirectories = resolveOperation.VisitedDirectories;
            if (!visitedDirectories.TryGetValue (parentDirectory.FilePath, out files)) {
                if (resolveOperation.Options.HasFlag (ResolveOperationOptions.SkipGacCache) ||
                    !GacCache.TryGetCachedFilePaths (parentDirectory.FilePath, out files)) {
                    try {
                        files = EnumerateAssembliesInDirectory (
                            parentDirectory.FilePath,
                            parentDirectory.ScanRecursively,
                            resolveOperation.CancellationToken);
                    } catch (Exception e) when (!(e is OperationCanceledException)) {
                        Log.Error (TAG, "exception when enumerating files in " +
                            $"'{parentDirectory.FilePath}'", e);
                        return null;
                    }
                }

                resolveOperation.VisitedDirectories = visitedDirectories.Add (
                    parentDirectory.FilePath,
                    files);
            }

            foreach (var file in files) {
                resolveOperation.CancellationToken.ThrowIfCancellationRequested ();

                var resolvedAssembly = ResolveFileForName (
                    resolveOperation,
                    file,
                    assemblyName,
                    checkExists: false);
                if (resolvedAssembly != null)
                    return resolvedAssembly;
            }

            return null;
        }

        ResolvedAssembly ResolveName (ResolveOperation resolveOperation, AssemblyName assemblyName)
        {
            foreach (var assemblySearchPath in resolveOperation.AssemblySearchPaths) {
                resolveOperation.CancellationToken.ThrowIfCancellationRequested ();

                if (!assemblySearchPath.FilePath.DirectoryExists)
                    continue;

                var resolvedAssembly = ResolveFirstInDirectoryForName (
                    resolveOperation,
                    assemblySearchPath,
                    assemblyName);
                if (resolvedAssembly != null)
                    return resolvedAssembly;
            }

            return null;
        }

        ResolvedAssembly ResolveFile (ResolveOperation resolveOperation, FilePath path, bool checkExists = true)
        {
            path = BaseDirectory.Combine (path);

            if (!IsAssemblyFile (path, checkExists))
                return null;

            if (resolveOperation.VisitedFiles.Contains (path))
                return null;

            resolveOperation.VisitedFiles = resolveOperation.VisitedFiles.Add (path);

            ResolvedAssembly resolvedAssembly;
            try {
                resolvedAssembly = ParseAssembly (resolveOperation, path);
            } catch {
                return null;
            }

            ResolvedAssembly cachedResolvedAssembly;
            if (resolveOperation.ResolvedAssemblies.TryGetValue (
                resolvedAssembly.AssemblyName, out cachedResolvedAssembly))
                return cachedResolvedAssembly;

            if (resolvedAssembly.References.Count > 0) {
                var parentDirectory = path.ParentDirectory;
                var pushPath = true;

                foreach (var assemblySearchPath in resolveOperation.AssemblySearchPaths) {
                    if (assemblySearchPath.FilePath == parentDirectory) {
                        pushPath = false;
                        break;
                    }
                }

                if (pushPath)
                    resolveOperation.AssemblySearchPaths
                        = resolveOperation.AssemblySearchPaths.Push (
                            new SearchPath { FilePath = parentDirectory });

                var resolvedReferences = ImmutableHashSet<ResolvedAssembly>.Empty;
                foreach (var name in resolvedAssembly.References) {
                    var resolvedReference = ResolveName (resolveOperation, name);
                    if (resolvedReference != null)
                        resolvedReferences = resolvedReferences.Add (resolvedReference);
                }

                if (resolvedReferences.Count > 0)
                    resolvedAssembly = resolvedAssembly.WithResolvedReferences (resolvedReferences);

                if (pushPath)
                    resolveOperation.AssemblySearchPaths
                        = resolveOperation.AssemblySearchPaths.Pop ();
            }

            // ResolveName above may have lead to this assembly being resolve anyway
            resolveOperation.ResolvedAssemblies = resolveOperation.ResolvedAssemblies.SetItem (
                resolvedAssembly.AssemblyName,
                resolvedAssembly);

            return resolvedAssembly;
        }

        ResolvedAssembly ParseAssembly (ResolveOperation resolveOperation, FilePath path)
        {
            using (var stream = File.OpenRead (path))
            using (var peReader = new PEReader (stream))
                return ParseAssembly (resolveOperation, path, peReader, peReader.GetMetadataReader ());
        }

        protected virtual ResolvedAssembly ParseAssembly (
            ResolveOperation resolveOperation,
            FilePath path,
            PEReader peReader,
            MetadataReader metadataReader)
        {
            var assemblyName = metadataReader
                .GetAssemblyDefinition ()
                .ReadAssemblyName (metadataReader);

            var references = ImmutableHashSet<AssemblyName>.Empty.WithComparer (
                ResolvedAssembly.NameEqualityComparer.Default);

            if (resolveOperation.Options.HasFlag (ResolveOperationOptions.ResolveReferences)) {
                foreach (var referenceHandle in metadataReader.AssemblyReferences) {
                    resolveOperation.CancellationToken.ThrowIfCancellationRequested ();

                    references = references.Add (metadataReader
                        .GetAssemblyReference (referenceHandle)
                        .ReadAssemblyName (metadataReader));
                }
            }

            return ResolvedAssembly.Create (path, assemblyName, references);
        }

        static bool IsAssemblyFile (FilePath path, bool checkExists = true) =>
            (String.Equals (path.Extension, ".dll", StringComparison.OrdinalIgnoreCase) ||
            String.Equals (path.Extension, ".exe", StringComparison.OrdinalIgnoreCase)) &&
            (!checkExists || path.FileExists);
    }
}