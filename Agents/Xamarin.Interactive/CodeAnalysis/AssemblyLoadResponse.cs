//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class AssemblyLoadResponse
    {
        public ResultItem [] LoadResults { get; set; }

        public bool InitializedAgentIntegration
            => LoadResults != null && LoadResults.Any (r => r.InitializedAgentIntegration);

        [Serializable]
        public struct ResultItem
        {
            public AssemblyIdentity AssemblyName { get; }
            public bool Success { get; }
            public bool InitializedAgentIntegration { get; }

            public ResultItem (AssemblyIdentity assemblyName, bool success, bool initializedAgentIntegration)
            {
                AssemblyName = assemblyName ?? throw new ArgumentNullException (nameof (assemblyName));
                Success = success;
                InitializedAgentIntegration = initializedAgentIntegration;
            }
        }
    }
}