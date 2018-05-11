// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;
using NuGet.Configuration;

namespace Xamarin.Interactive.NuGet
{
    public sealed class InteractivePackageSource
    {
        public string Source { get; }

        [JsonConstructor]
        public InteractivePackageSource (string source)
            => Source = source ?? throw new ArgumentNullException (nameof (source));

        public static InteractivePackageSource FromPackageSource (PackageSource packageSource)
            => packageSource == null
                ? null
                : new InteractivePackageSource (packageSource.Source);

        public PackageSource ToPackageSource ()
            => new PackageSource (Source);
    }
}