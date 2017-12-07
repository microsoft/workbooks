//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using NuGet.Packaging.Core;

namespace Xamarin.Interactive.NuGet
{
    sealed class PackageIdComparer : IEqualityComparer<InteractivePackage>, IEqualityComparer<PackageDependency>
    {
        public static bool Equals (string x, string y)
            => string.Equals (x, y, StringComparison.OrdinalIgnoreCase);

        public static bool Equals (PackageIdentity x, PackageIdentity y)
            => Equals (x?.Id, y?.Id);

        public static bool Equals (InteractivePackage x, InteractivePackage y)
            => Equals (x?.Identity, y?.Identity);

        public static bool Equals (PackageDependency x, PackageDependency y)
            => Equals (x?.Id, y?.Id);

        public static readonly PackageIdComparer Default = new PackageIdComparer ();

        bool IEqualityComparer<InteractivePackage>.Equals (InteractivePackage x, InteractivePackage y)
            => Equals (x, y);

        bool IEqualityComparer<PackageDependency>.Equals (PackageDependency x, PackageDependency y)
            => Equals (x, y);

        int IEqualityComparer<InteractivePackage>.GetHashCode (InteractivePackage obj)
            => obj?.Identity.Id == null ? 0 : obj.Identity.Id.GetHashCode ();

        int IEqualityComparer<PackageDependency>.GetHashCode (PackageDependency obj)
            => obj?.Id == null ? 0 : obj.Id.GetHashCode ();
    }
}