//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    public sealed class ResolvedAssembly : IEquatable<ResolvedAssembly>
    {
        public sealed class NameEqualityComparer : IEqualityComparer<ResolvedAssembly>, IEqualityComparer<AssemblyName>
        {
            public static readonly NameEqualityComparer Default = new NameEqualityComparer ();

            public bool Equals (ResolvedAssembly x, ResolvedAssembly y)
                => ReferenceEquals (x, y) || Equals (x?.AssemblyName, y?.AssemblyName);

            public bool Equals (AssemblyName x, AssemblyName y)
                => ReferenceEquals (x, y) || String.Equals (
                    x?.Name,
                    y?.Name,
                    StringComparison.OrdinalIgnoreCase);

            public int GetHashCode (ResolvedAssembly obj)
                => GetHashCode (obj?.AssemblyName);

            public int GetHashCode (AssemblyName obj)
                => (obj?.Name?.GetHashCode ()).GetValueOrDefault ();
        }

        public static ResolvedAssembly Create (
            string path,
            AssemblyName assemblyName,
            IEnumerable<AssemblyName> references = null,
            IEnumerable<ResolvedAssembly> resolvedReferences = null,
            IEnumerable<ExternalDependency> externalDependencies = null,
            bool hasIntegration = false)
            => new ResolvedAssembly (
                path,
                assemblyName,
                hasIntegration,
                references,
                resolvedReferences,
                externalDependencies);

        public FilePath Path { get; }
        public AssemblyName AssemblyName { get; }
        public bool HasIntegration { get; }
        public ImmutableHashSet<AssemblyName> References { get; }
        public ImmutableHashSet<ResolvedAssembly> ResolvedReferences { get; }
        public ImmutableArray<ExternalDependency> ExternalDependencies { get; }

        ResolvedAssembly (
            string path,
            AssemblyName assemblyName,
            bool hasIntegration,
            IEnumerable<AssemblyName> references = null,
            IEnumerable<ResolvedAssembly> resolvedReferences = null,
            IEnumerable<ExternalDependency> externalDependencies = null)
        {
            if (path == null)
                throw new ArgumentNullException (nameof (path));

            if (assemblyName == null)
                throw new ArgumentNullException (nameof (assemblyName));

            Path = path;
            AssemblyName = assemblyName;
            HasIntegration = hasIntegration;
            References = references?.ToImmutableHashSet ()
                ?? ImmutableHashSet<AssemblyName>.Empty;
            ResolvedReferences = resolvedReferences?.ToImmutableHashSet ()
                ?? ImmutableHashSet<ResolvedAssembly>.Empty;
            ExternalDependencies = externalDependencies?.ToImmutableArray ()
                ?? ImmutableArray<ExternalDependency>.Empty;
        }

        public ResolvedAssembly With (
            bool? hasIntegration = null,
            IEnumerable<AssemblyName> references = null,
            IEnumerable<ResolvedAssembly> resolvedReferences = null,
            IEnumerable<ExternalDependency> externalDependencies = null)
            => new ResolvedAssembly (
                Path,
                AssemblyName,
                hasIntegration ?? HasIntegration,
                references ?? References,
                resolvedReferences ?? ResolvedReferences,
                externalDependencies ?? ExternalDependencies);

        public ResolvedAssembly WithHasIntegration (bool hasIntegration)
            => With (hasIntegration: hasIntegration);

        public ResolvedAssembly WithReferences (IEnumerable<AssemblyName> references)
            => With (references: references);

        public ResolvedAssembly WithResolvedReferences (IEnumerable<ResolvedAssembly> resolvedReferences)
            => With (resolvedReferences: resolvedReferences);

        public ResolvedAssembly WithExternalDependencies (IEnumerable<ExternalDependency> externalDependencies)
            => With (externalDependencies: externalDependencies);

        public bool Equals (ResolvedAssembly obj)
            => obj != null && obj.Path == Path;

        public override bool Equals (object obj)
            => obj is ResolvedAssembly && Equals (((ResolvedAssembly)obj));

        public override int GetHashCode ()
            => Path.GetHashCode ();

        public void Dump (TextWriter writer)
            => Dump (writer, 0);

        void Dump (TextWriter writer, int depth)
        {
            if (writer == null)
                throw new ArgumentNullException (nameof (writer));

            var pad = String.Empty.PadLeft (depth * 2);

            writer.WriteLine ($"{pad}{AssemblyName} ({Path.FullPath})");

            foreach (var reference in ResolvedReferences)
                reference.Dump (writer, depth + 1);
        }
    }
}