//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NuGet.Packaging.Core
{
    static class PackageIdentityExtensions
    {
        public static string GetFullName (this PackageIdentity package)
            => $"{package.Id} {package.Version?.ToNormalizedString ()}";
    }
}
