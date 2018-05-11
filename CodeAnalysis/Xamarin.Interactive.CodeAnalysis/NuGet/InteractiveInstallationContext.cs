//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Xamarin.Interactive.NuGet
{
    /// <summary>
    /// Tracks installation state for InteractivePackageManager.
    /// </summary>
    sealed class InteractiveInstallationContext
    {
        public ImmutableHashSet<InteractivePackage> InstalledPackages { get; private set; } =
            ImmutableHashSet<InteractivePackage>
                .Empty
                .WithComparer (PackageIdComparer.Default);

        public void AddInstalledPackage (InteractivePackage package)
        {
            if (package == null)
                return;
            InstalledPackages = InstalledPackages
                .Remove (package)
                .Add (package);
        }
    }
}
