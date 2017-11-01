//
// AssemblyLoadResponse.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Linq;

using Xamarin.Interactive.Representations.Reflection;

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
            public RepresentedAssemblyName AssemblyName { get; }
            public bool Success { get; }
            public bool InitializedAgentIntegration { get; }

            public ResultItem (RepresentedAssemblyName assemblyName, bool success, bool initializedAgentIntegration)
            {
                AssemblyName = assemblyName ?? throw new ArgumentNullException (nameof (assemblyName));
                Success = success;
                InitializedAgentIntegration = initializedAgentIntegration;
            }
        }
    }
}