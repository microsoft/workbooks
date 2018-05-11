//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.NuGet
{
    interface IPackageManagerView
    {
        void ClearPackages ();
        void AddPackageResult (PackageViewModel package);
    }
}
