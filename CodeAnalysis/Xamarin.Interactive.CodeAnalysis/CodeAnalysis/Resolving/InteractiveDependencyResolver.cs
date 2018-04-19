//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Microsoft.CodeAnalysis;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

using AssemblyIdentity = Xamarin.Interactive.CodeAnalysis.Resolving.AssemblyIdentity;
using AssemblyDefinition = Xamarin.Interactive.CodeAnalysis.Resolving.AssemblyDefinition;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    public class InteractiveDependencyResolver : NativeDependencyResolver
    {
        const string TAG = nameof (InteractiveDependencyResolver);

        ImmutableArray<AssemblyDefinition> defaultReferences;

        // HACK: This is a temporary fix to get iOS agent/app assemblies sent to the
        //       Windows client when using the remote sim.
        readonly string assemblyTempDir = Path.Combine (
            Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData),
            "Xamarin",
            "Inspector",
            "Remote Assembly Temp");

        internal InteractiveDependencyResolver (AgentType agentType) : base (agentType)
        {
            var consoleOrWpf = AgentType == AgentType.WPF || AgentType == AgentType.Console;;
            UseGacCache = consoleOrWpf || AgentType == AgentType.MacNet45;

            AddAssemblySearchPath (assemblyTempDir);
            try {
                Directory.Delete (assemblyTempDir, recursive: true);
            } catch {
            }
        }

        protected override ResolvedAssembly ParseAssembly (
            ResolveOperation resolveOperation,
            FilePath path,
            PEReader peReader,
            MetadataReader metadataReader)
        {
            var resolvedAssembly = base.ParseAssembly (resolveOperation, path, peReader, metadataReader);

            foreach (var attrHandle in metadataReader.GetAssemblyDefinition ().GetCustomAttributes ()) {
                var attr = metadataReader.GetCustomAttribute (attrHandle);
                if (attr.Constructor.Kind != HandleKind.MemberReference)
                    continue;

                var ctor = metadataReader.GetMemberReference ((MemberReferenceHandle)attr.Constructor);
                var typeReference = metadataReader.GetTypeReference ((TypeReferenceHandle)ctor.Parent);

                var integrationType = typeof (EvaluationContextManagerIntegrationAttribute);
                if (metadataReader.GetString (typeReference.Namespace) == integrationType.Namespace &&
                    metadataReader.GetString (typeReference.Name) == integrationType.Name)
                    return resolvedAssembly.With (
                        hasIntegration: true,
                        externalDependencies: resolvedAssembly.ExternalDependencies);
            }

            return resolvedAssembly;
        }

        public void AddDefaultReferences (IEnumerable<AssemblyDefinition> defaultReferences)
            => this.defaultReferences = defaultReferences.ToImmutableArray ();

        public ImmutableArray<ResolvedAssembly> ResolveDefaultReferences ()
        {
            foreach (var defaultReference in defaultReferences) {
                if (defaultReference.Content.PEImage == null)
                    continue;

                CacheRemoteAssembly (defaultReference);
            }

            return Resolve (defaultReferences);
        }

        public string CacheRemoteAssembly (AssemblyDefinition remoteAssembly)
        {
            if (remoteAssembly.Content.PEImage == null)
                throw new ArgumentException ("PEImage must not be null", nameof (remoteAssembly));

            Directory.CreateDirectory (assemblyTempDir);

            var path = Path.Combine (
                assemblyTempDir,
                Path.GetFileName (remoteAssembly.Content.Location));

            if (!File.Exists (path))
                File.WriteAllBytes (path, remoteAssembly.Content.PEImage);

            return path;
        }

        public Task<AssemblyDefinition []> ResolveReferencesAsync (IEnumerable<FilePath> references,
            bool includePeImages, CancellationToken cancellationToken)
        {
            if (references == null || !references.Any ())
                return Task.FromResult (Array.Empty <AssemblyDefinition> ());

            return Task.Run (() => {
                var stopwatch = new Stopwatch ();
                stopwatch.Start ();

                var resolvedReferences = Resolve (references, cancellationToken: cancellationToken)
                    .Select (r => {
                        var syms = includePeImages ? GetDebugSymbolsFromAssemblyPath (r.Path) : null;
                        var peImage = includePeImages ? GetFileBytes (r.Path) : null;
                        var externalDeps = r.ExternalDependencies
                            .Select (d => new AssemblyDependency (
                                d.Location,
                                includePeImages ? GetFileBytes (d.Location) : null))
                            .ToArray ();
                        return new AssemblyDefinition (
                            r.AssemblyName,
                            r.Path,
                            hasIntegration: r.HasIntegration,
                            peImage: peImage,
                            debugSymbols: syms,
                            externalDependencies: externalDeps
                        );
                    })
                    .ToArray ();

                stopwatch.Stop ();

                Log.Debug (TAG, $"Resolved {resolvedReferences.Length} references in {stopwatch.Elapsed}s");

                return resolvedReferences;
            }, cancellationToken);
        }

        public static byte [] GetFileBytes (FilePath path)
        {
            try {
                return File.ReadAllBytes (path);
            } catch (Exception e) {
                Log.Warning (TAG, $"Could not read assembly at {path}", e);
                return null;
            }
        }

        public static byte [] GetDebugSymbolsFromAssemblyPath (FilePath path)
        {
            try {
                // Prefer MDBs, then PDBs. Mono outputs Foo.dll.mdb, so change the extension to that.
                var mdbPath = path.ChangeExtension (".dll.mdb");
                if (mdbPath.FileExists)
                    return File.ReadAllBytes (mdbPath);
                // This is intentionally not using mdbPath, because MS .NET names the symbol files
                // Foo.pdb, compared to Mono's Foo.dll.mdb.
                var pdbPath = path.ChangeExtension (".pdb");
                return pdbPath.FileExists ? File.ReadAllBytes (pdbPath) : null;
            } catch (Exception e) {
                Log.Warning (TAG, $"Could not get debug symbols for assembly at {path}", e);
                return null;
            }
        }
    }
}