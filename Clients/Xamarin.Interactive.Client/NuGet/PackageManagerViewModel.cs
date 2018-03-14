//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Protocol.Core.Types;

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.NuGet
{
    class PackageManagerViewModel
    {
        const string TAG = nameof (PackageManagerViewModel);
        public const string IntegrationPackageId = "Xamarin.Workbooks.Integration";

        readonly ClientSession clientSession;
        readonly IPackageManagerView packageManagerView;

        public PackageSourceViewModel [] PackageSources { get; }

        public PackageManagerViewModel (ClientSession clientSession, IPackageManagerView packageManagerView)
        {
            if (clientSession == null)
                throw new ArgumentNullException (nameof (clientSession));

            if (packageManagerView == null)
                throw new ArgumentNullException (nameof (packageManagerView));

            this.clientSession = clientSession;
            this.packageManagerView = packageManagerView;

            PackageSources = clientSession
                .Workbook
                .Packages
                .SourceRepositories
                .Select (repo => new PackageSourceViewModel (repo))
                .ToArray ();
        }

        public async Task SearchForPackagesAsync (
            string searchTerm,
            bool allowPrereleaseVersions,
            PackageSourceViewModel sourceViewModel,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            packageManagerView.ClearPackages ();

            var repository = sourceViewModel?.SourceRepository;

            if (string.IsNullOrWhiteSpace (searchTerm) || repository == null)
                return;

            var targetfx = new [] {
                clientSession.Workbook.Packages.TargetFramework.DotNetFrameworkName,
            };

            if (cancellationToken.IsCancellationRequested)
                return;

            try {
                var searchResource =
                    await repository.GetResourceAsync<PackageSearchResource> (cancellationToken);
                var searchFilter = new SearchFilter (allowPrereleaseVersions) {
                    SupportedFrameworks = targetfx,
                };

                const int pageSize = 100; // Totally abritrary. "json" search has about 1500 results
                var skip = 0;

                while (true) {
                    var resultSetSize = 0;

                    foreach (var searchMetadata in await searchResource.SearchAsync (
                        searchTerm,
                        searchFilter,
                        skip,
                        pageSize,
                        clientSession.Workbook.Packages.Logger,
                        cancellationToken)) {

                        if (cancellationToken.IsCancellationRequested)
                            return;

                        resultSetSize++;

                        if (searchMetadata.Identity.Id == IntegrationPackageId)
                            continue;

                        // In the future, pass the full metadata to the views so they can show
                        // icons, descriptions, authors, licenses, etc
                        packageManagerView.AddPackageResult (new PackageViewModel (
                            searchMetadata.Identity,
                            repository));
                    }

                    if (resultSetSize < pageSize)
                        break;

                    skip += resultSetSize;
                }
            } catch (FatalProtocolException) when (cancellationToken.IsCancellationRequested) {
                // NuGet throws weird FatalProtocolException when cancellation requested:
                // https://github.com/NuGet/NuGet.Client/blob/4.0.0-rc4/src/NuGet.Core/NuGet.Protocol.Core.v3/Resources/RawSearchResourceV3.cs#L110
                return;
            }
        }

        public async Task AddPackageAsync (
            PackageViewModel packageViewModel,
            CancellationToken cancellationToken)
        {
            if (packageViewModel == null)
                throw new ArgumentNullException (nameof (packageViewModel));

            await clientSession.PackageManager.InstallAsync (
                InteractivePackageDescription.FromPackageViewModel (packageViewModel),
                cancellationToken);
        }
    }
}