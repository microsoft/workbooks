//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    // Implement a custom AssemblyName comparer so that we don't have to insert
    // multiple different varieties of the same assembly name into the dictionary.
    // Different pieces of external code seem to look up our submission assemblies in
    // different ways: JSON.NET uses bare names (see https://bugzilla.xamarin.com/show_bug.cgi?id=58801),
    // most of the framework uses fully qualified assembly names, and ASP.NET Core
    // seems to use fully-qualified-except-no-version names. As submission assemblies
    // aren't versioned, don't have a culture, and don't have a public key token, treating
    // the name in a case insensitive way is fine.
    sealed class AssemblyNameInsensitiveNameOnlyComparer : IEqualityComparer<AssemblyName>, IComparer<AssemblyName>
    {
        public static bool Equals (string x, string y)
            => string.Equals (x, y, StringComparison.OrdinalIgnoreCase);

        public static bool Equals (AssemblyName x, AssemblyName y)
            => Equals (x?.Name, y?.Name);

        public static readonly IComparer<AssemblyName> Default
            = new AssemblyNameInsensitiveNameOnlyComparer ();

        bool IEqualityComparer<AssemblyName>.Equals (AssemblyName x, AssemblyName y)
            => Equals (x?.Name, y?.Name);

        int IEqualityComparer<AssemblyName>.GetHashCode (AssemblyName obj)
            => obj?.Name == null ? 0 : obj.Name.GetHashCode ();

        public int Compare (AssemblyName x, AssemblyName y)
            => string.Compare (x?.Name, y?.Name, StringComparison.OrdinalIgnoreCase);
    }

    sealed class AssemblyNameFullNameComparer : IEqualityComparer<AssemblyName>, IComparer<AssemblyName>
    {
        public static bool Equals (string x, string y)
            => string.Equals (x, y, StringComparison.OrdinalIgnoreCase);

        public static bool Equals (AssemblyName x, AssemblyName y)
            => Equals (x?.Name, y?.Name);

        public static readonly IComparer<AssemblyName> Default
            = new AssemblyNameFullNameComparer ();

        bool IEqualityComparer<AssemblyName>.Equals (AssemblyName x, AssemblyName y)
            => Equals (x?.FullName, y?.FullName);

        int IEqualityComparer<AssemblyName>.GetHashCode (AssemblyName obj)
            => obj?.Name == null ? 0 : obj.Name.GetHashCode ();

        public int Compare (AssemblyName x, AssemblyName y)
            => string.Compare (x?.FullName, y?.FullName);
    }
}