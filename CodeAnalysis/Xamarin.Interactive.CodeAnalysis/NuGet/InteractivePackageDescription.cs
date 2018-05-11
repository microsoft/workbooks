// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Xamarin.Interactive.NuGet
{
    public sealed class InteractivePackageDescription
    {
        public string PackageId { get; }
        public string IdentityVersion { get; }
        public string VersionRange { get; }
        public bool IsExplicitlySelected { get; }
        public InteractivePackageSource Source { get; }

        [JsonConstructor]
        public InteractivePackageDescription (
            string packageId,
            string identityVersion = null,
            string versionRange = null,
            bool isExplicitlySelected = true,
            InteractivePackageSource source = null)
        {
            PackageId = packageId
                ?? throw new ArgumentNullException (nameof (packageId));

            IdentityVersion = identityVersion;
            VersionRange = versionRange;
            IsExplicitlySelected = isExplicitlySelected;
            Source = source;
        }

        public SourceRepository GetSourceRepository ()
            => null;

        internal static InteractivePackageDescription FromInteractivePackage (InteractivePackage package)
            => new InteractivePackageDescription (
                package.Identity.Id,
                package.Identity.HasVersion
                    ? package.Identity.Version.ToString ()
                    : null,
                package.SupportedVersionRange == null
                    ? null
                    : package.SupportedVersionRange.OriginalString
                        ?? package.SupportedVersionRange.ToNormalizedString (),
                package.IsExplicit);

        internal InteractivePackage ToInteractivePackage ()
            => VersionRange == null
                ? new InteractivePackage (
                    ToPackageIdentity (),
                    IsExplicitlySelected)
                : new InteractivePackage (
                    PackageId,
                    global::NuGet.Versioning.VersionRange.Parse (VersionRange),
                    IsExplicitlySelected);

        internal PackageIdentity ToPackageIdentity ()
            => new PackageIdentity (
                PackageId,
                IdentityVersion == null
                    ? null
                    : NuGetVersion.Parse (IdentityVersion));

        internal static InteractivePackageDescription FromPackageViewModel (PackageViewModel package)
            => new InteractivePackageDescription (
                package.Package.Id,
                package.Package.HasVersion
                    ? package.Package.Version.ToString ()
                    : null,
                null,
                true,
                InteractivePackageSource.FromPackageSource (package.SourceRepository?.PackageSource));

        internal PackageViewModel ToPackageViewModel ()
            => new PackageViewModel (ToPackageIdentity ());
    }
}