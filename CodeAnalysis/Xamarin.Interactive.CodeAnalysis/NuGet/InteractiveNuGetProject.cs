//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.NuGet
{
    /// <summary>
    /// A NuGet Project that extracts references MSBuild-style in a way that an InteractivePackageManager can
    /// consume. Doesn't actually install anything anywhere. Packages end up being referenced from the user's
    /// global package cache (typically ~/.nuget/packages).
    /// </summary>
    sealed class InteractiveNuGetProject : NuGetProject
    {
        readonly FilePath packagesDirectory;

        public NuGetFramework TargetFramework { get; }

        public InteractiveNuGetProject (NuGetFramework framework, ISettings settings)
        {
            if (framework == null)
                throw new ArgumentNullException (nameof (framework));

            if (settings == null)
                throw new ArgumentNullException (nameof (settings));

            packagesDirectory = SettingsUtility.GetGlobalPackagesFolder (settings);

            TargetFramework = framework;
            InternalMetadata [NuGetProjectMetadataKeys.TargetFramework] = framework;
            InternalMetadata [NuGetProjectMetadataKeys.Name] = nameof (InteractiveNuGetProject);
        }

        public string Name => (string)InternalMetadata [NuGetProjectMetadataKeys.Name];

        public override Task<bool> InstallPackageAsync (
            PackageIdentity packageIdentity,
            DownloadResourceResult downloadResourceResult,
            INuGetProjectContext nuGetProjectContext,
            CancellationToken token)
            => Task.FromResult (true);

        public override Task<bool> UninstallPackageAsync (
            PackageIdentity packageIdentity,
            INuGetProjectContext nuGetProjectContext,
            CancellationToken token)
            => Task.FromResult (true);

        public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationToken token)
            => Task.FromResult (Enumerable.Empty<PackageReference> ());

        public FilePath GetInstalledPath (PackageIdentity packageIdentity)
        {
            if (packageIdentity == null)
                throw new ArgumentNullException (nameof (packageIdentity));

            if (!packageIdentity.HasVersion)
                throw new ArgumentException ("PackageIdentity.Version must be set");

            return packagesDirectory.Combine (
                packageIdentity.Id.ToLower (),
                packageIdentity.Version.ToNormalizedString ());
        }
    }
}