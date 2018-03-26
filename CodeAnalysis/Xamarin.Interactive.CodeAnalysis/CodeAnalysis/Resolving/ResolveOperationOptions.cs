//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [Flags]
    public enum ResolveOperationOptions
    {
        None = 0,
        ResolveReferences = 1 << 0,
        SkipGacCache = 1 << 1
    }
}