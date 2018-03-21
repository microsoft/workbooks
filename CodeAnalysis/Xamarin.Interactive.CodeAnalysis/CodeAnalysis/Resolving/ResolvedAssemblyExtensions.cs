//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    static class ResolvedAssemblyExtensions
    {
        public static ImmutableArray<ResolvedAssembly> TopologicallySorted (
            this IEnumerable<ResolvedAssembly> resolvedAssemblies,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            var seenResolvedAssemblies = new HashSet<ResolvedAssembly> ();
            var resultList = new List<ResolvedAssembly> ();
            TopologicalSort (
                resolvedAssemblies,
                seenResolvedAssemblies,
                resultList,
                cancellationToken);
            return resultList.ToImmutableArray ();
        }

        static void TopologicalSort (
            IEnumerable<ResolvedAssembly> resolvedAssemblies,
            HashSet<ResolvedAssembly> seenResolvedAssemblies,
            List<ResolvedAssembly> resultList,
            CancellationToken cancellationToken)
        {
            foreach (var resolvedAssembly in resolvedAssemblies) {
                cancellationToken.ThrowIfCancellationRequested ();

                if (!seenResolvedAssemblies.Add (resolvedAssembly))
                    continue;

                if (resolvedAssembly.ResolvedReferences.Count > 0)
                    TopologicalSort (
                        resolvedAssembly.ResolvedReferences,
                        seenResolvedAssemblies,
                        resultList,
                        cancellationToken);

                resultList.Add (resolvedAssembly);
            }
        }
    }
}