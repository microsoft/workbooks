// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.NuGet
{
    sealed class PackageIdComparer : IEqualityComparer<InteractivePackageDescription>
    {
        public static readonly PackageIdComparer Default = new PackageIdComparer ();

        public static bool Equals (string x, string y)
            => string.Equals (x, y, StringComparison.OrdinalIgnoreCase);

        public bool Equals (InteractivePackageDescription x, InteractivePackageDescription y)
            => Equals (x.PackageId, y.PackageId);

        public int GetHashCode (InteractivePackageDescription obj)
            => obj.PackageId == null ? 0 : obj.PackageId.GetHashCode ();
    }
}