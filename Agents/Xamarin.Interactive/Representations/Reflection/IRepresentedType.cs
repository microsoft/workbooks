//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Representations.Reflection
{
    public interface IRepresentedType
    {
        string Name { get; }
        Type ResolvedType { get; }
        IRepresentedType BaseType { get; }
    }
}