//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class AssemblyLoadResponse
    {
        public IReadOnlyList<AssemblyLoadResult> LoadResults { get; }

        public AssemblyLoadResponse (AssemblyLoadResult [] loadResults)
            => LoadResults = loadResults;
    }
}