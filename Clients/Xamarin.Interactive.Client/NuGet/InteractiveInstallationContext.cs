//
// InteractiveInstallationContext.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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
