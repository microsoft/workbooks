// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    public struct AssemblyLoadResult
    {
        public AssemblyIdentity AssemblyName { get; }
        public bool Success { get; }
        public bool InitializedAgentIntegration { get; }

        public AssemblyLoadResult (
            AssemblyIdentity assemblyName,
            bool success,
            bool initializedAgentIntegration)
        {
            AssemblyName = assemblyName
                ?? throw new ArgumentNullException (nameof (assemblyName));
            Success = success;
            InitializedAgentIntegration = initializedAgentIntegration;
        }
    }

    static class AssemblyLoadResultExtensions
    {
        public static bool InitializedAgentIntegration (
            this IReadOnlyCollection<AssemblyLoadResult> results)
            => results != null && results.Any (r => r.InitializedAgentIntegration);
    }
}