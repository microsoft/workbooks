// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    static class DependencyResolverExtensions
    {
        public static Task<AssemblyDefinition []> ResolveReferencesAsync (
            this InteractiveDependencyResolver dependencyResolver,
            IEnumerable<MetadataReference> references,
            bool includePeImages,
            CancellationToken cancellationToken)
            => dependencyResolver.ResolveReferencesAsync (
                references
                    .OfType<PortableExecutableReference> ()
                    .Select (r => new FilePath (r.FilePath)),
                includePeImages,
                cancellationToken);
    }
}