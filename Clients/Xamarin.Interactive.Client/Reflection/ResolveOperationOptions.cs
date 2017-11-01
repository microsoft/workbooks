//
// ResolveOperationOptions.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Reflection
{
    [Flags]
    enum ResolveOperationOptions
    {
        None = 0,
        ResolveReferences = 1 << 0,
        SkipGacCache = 1 << 1
    }
}