//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
    public interface IAssemblyIdentity : IEquatable<IAssemblyIdentity>
    {
        string Name { get; }
        string FullName { get; }
        Version Version { get; }
    }
}