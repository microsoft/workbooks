// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using NVR = NuGet.Versioning.VersionRange;

namespace Xamarin.Interactive.NuGet
{
    public struct InteractivePackageDescription : IEquatable<InteractivePackageDescription>
    {
        public string PackageId { get; }
        public string VersionRange { get; }

        public InteractivePackageDescription (
            string packageId,
            string versionRange)
        {
            PackageId = packageId
                ?? throw new ArgumentNullException (nameof (packageId));

            VersionRange = versionRange;
        }

        public bool Equals (InteractivePackageDescription other)
        {
            if (!PackageIdComparer.Equals (PackageId, other.PackageId))
                return false;

            return VersionRange == other.VersionRange || (NVR.TryParse (VersionRange, out var a) ==
                NVR.TryParse (other.VersionRange, out var b) && a?.Equals (b) == true);
        }

        public override int GetHashCode ()
            => Hash.Combine (PackageId, VersionRange);

        public void Deconstruct (out string packageId, out string versionRange)
        {
            packageId = PackageId;
            versionRange = VersionRange;
        }

        public override string ToString ()
            => string.IsNullOrEmpty (VersionRange) ? PackageId : $"{PackageId}.{VersionRange}";

        public static implicit operator InteractivePackageDescription ((string packageId, string version) description)
            => new InteractivePackageDescription (description.packageId, description.version);
    }
}