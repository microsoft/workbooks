// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.NuGet
{
    sealed class InteractivePackage
    {
        /// <summary>
        /// The resolved/installed identity for a package. If set, takes precedence
        /// over <see cref="PackageReference"/> when performing a restore operation.
        /// </summary>
        public PackageIdentity Identity { get; }

        /// <summary>
        /// If set, represents the user-supplied description of a package to restore,
        /// from which <see cref="Identity"/> will be computed.
        /// </summary>
        public InteractivePackageDescription PackageReference { get; }

        /// <summary>
        /// The set of fully resolved assembly reference paths for the package after
        /// it has been restored.
        /// </summary>
        public IReadOnlyList<FilePath> AssemblyReferences { get; }

        public InteractivePackage (
            PackageIdentity identity,
            InteractivePackageDescription packageReference,
            IReadOnlyList<FilePath> assemblyReferences)
        {
            if (identity != null && !identity.HasVersion)
                throw new ArgumentException (
                    "identity must have a Version",
                    nameof (identity));

            if (identity == null && packageReference.Equals (default))
                throw new ArgumentNullException (
                    $"must provide either a {nameof (identity)} or a {nameof (packageReference)}");

            Identity = identity;
            PackageReference = packageReference;
            AssemblyReferences = assemblyReferences;
        }

        internal LibraryDependency ToLibraryDependency ()
        {
            LibraryRange ToLibraryRange ()
            {
                if (Identity != null)
                    return new LibraryRange (
                        Identity.Id,
                        new VersionRange (Identity.Version),
                        LibraryDependencyTarget.Package);

                if (string.IsNullOrEmpty (PackageReference.VersionRange))
                    return new LibraryRange (
                        PackageReference.PackageId,
                        LibraryDependencyTarget.Package);

                return new LibraryRange (
                    PackageReference.PackageId,
                    VersionRange.Parse (PackageReference.VersionRange),
                    LibraryDependencyTarget.Package);
            }

            return new LibraryDependency {
                AutoReferenced = PackageReference.Equals (default),
                LibraryRange = ToLibraryRange ()
            };
        }

        public static InteractivePackage FromPackageReference (InteractivePackageDescription packageReference)
            => new InteractivePackage (null, packageReference, null);
    }
}