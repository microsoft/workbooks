//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class AssemblyLoadResponse
    {
        public IReadOnlyList<AssemblyLoadResult> LoadResults { get; }

        [JsonConstructor]
        public AssemblyLoadResponse (IReadOnlyList<AssemblyLoadResult> loadResults)
            => LoadResults = loadResults;
    }
}