// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.NuGet
{
    sealed class PackageIdVersionComparer : IEqualityComparer<InteractivePackage>
    {
        public static readonly PackageIdVersionComparer Default = new PackageIdVersionComparer ();

        bool IEqualityComparer<InteractivePackage>.Equals (InteractivePackage x, InteractivePackage y)
            => PackageIdComparer.Equals (x, y) && x?.Identity.Version == y?.Identity.Version;

        int IEqualityComparer<InteractivePackage>.GetHashCode (InteractivePackage obj)
            => Hash.Combine (
                obj?.Identity.Id == null ? 0 : obj.Identity.Id.GetHashCode (),
                obj?.Identity.Version == null ? 0 : obj.Identity.Version.GetHashCode ());
    }
}