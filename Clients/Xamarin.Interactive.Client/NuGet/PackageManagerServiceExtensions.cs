// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.NuGet
{
    static class PackageManagerServiceExtensions
    {
        public static Task InitializeAsync (
            this PackageManagerService service,
            Sdk targetSdk,
            IEnumerable<InteractivePackageDescription> initialPackages = null,
            CancellationToken cancellationToken = default)
            => service.InitializeAsync (
                targetSdk,
                ClientApp
                    .SharedInstance
                    .Paths
                    .CacheDirectory
                    .Combine ("package-manager"),
                initialPackages,
                cancellationToken);
    }
}