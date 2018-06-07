// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.NuGet
{
    /// <summary>
    /// An immutable collection for managing a set of package references keyed on the package
    /// identifier. Only one package with the same identifier may exist in the list.
    /// </summary>
    public sealed class PackageReferenceList : IReadOnlyList<InteractivePackageDescription>
    {
        public static PackageReferenceList Empty { get; } = new PackageReferenceList (
            ImmutableList<InteractivePackageDescription>.Empty);

        public static PackageReferenceList Create (params InteractivePackageDescription [] packageReferences)
            => Empty.ReplaceAllWith (packageReferences);

        public static PackageReferenceList Create (IEnumerable<InteractivePackageDescription> packageReferences)
            => Empty.ReplaceAllWith (packageReferences);

        readonly ImmutableList<InteractivePackageDescription> packages;

        public int Count => packages.Count;
        public InteractivePackageDescription this [int index] => packages [index];

        PackageReferenceList (ImmutableList<InteractivePackageDescription> packages)
            => this.packages = packages;

        public bool TryGetValue (string packageId, out InteractivePackageDescription packageReference)
        {
            if (packageId == null) {
                packageReference = default;
                return false;
            }

            packageReference = packages.Find (p => PackageIdComparer.Equals (p.PackageId, packageId));
            return !packageReference.Equals (default);
        }

        PackageReferenceList Update (ImmutableList<InteractivePackageDescription> updatedPackages)
            => updatedPackages == this.packages
                ? this
                : new PackageReferenceList (updatedPackages);

        public PackageReferenceList AddOrUpdate (InteractivePackageDescription package)
            => AddOrUpdate (new [] { package });

        public PackageReferenceList AddOrUpdate (IEnumerable<InteractivePackageDescription> packages)
        {
            if (packages == null)
                throw new ArgumentNullException (nameof (packages));

            var updatedPackages = this.packages;

            foreach (var package in packages) {
                var index = updatedPackages.IndexOf (package, PackageIdComparer.Default);
                if (index < 0)
                    updatedPackages = updatedPackages.Add (package);
                else if (!package.Equals (updatedPackages [index]))
                    updatedPackages = updatedPackages.SetItem (index, package);
            }

            return Update (updatedPackages);
        }

        public PackageReferenceList Remove (InteractivePackageDescription package)
            => Remove (new [] { package });

        public PackageReferenceList Remove (IEnumerable<InteractivePackageDescription> packages)
        {
            if (packages == null)
                throw new ArgumentNullException (nameof (packages));

            var updatedPackages = this.packages;

            foreach (var package in packages) {
                var index = updatedPackages.IndexOf (package, PackageIdComparer.Default);
                if (index >= 0)
                    updatedPackages = updatedPackages.RemoveAt (index);
            }

            return Update (updatedPackages);
        }

        public PackageReferenceList ReplaceAllWith (IEnumerable<InteractivePackageDescription> packages)
        {
            if (packages == null)
                throw new ArgumentNullException (nameof (packages));

            var candidatePackages = ImmutableList.CreateRange (packages);

            if (this.packages.Count != candidatePackages.Count)
                return Update (candidatePackages);

            for (int i = 0; i < candidatePackages.Count; i++) {
                if (!this.packages [i].Equals (candidatePackages [i]))
                    return Update (candidatePackages);
            }

            return this;
        }

        public PackageReferenceList Clear ()
            => packages.Count == 0
                ? this
                : new PackageReferenceList (packages.Clear ());

        public IEnumerator<InteractivePackageDescription> GetEnumerator ()
            => packages.GetEnumerator ();

        IEnumerator IEnumerable.GetEnumerator ()
            => GetEnumerator ();
    }
}