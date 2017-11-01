//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace Xamarin.Interactive.NuGet
{
    sealed class PackageViewModel
    {
        public PackageIdentity Package { get; }

        public SourceRepository SourceRepository { get; }

        public string DisplayName { get; }

        public PackageViewModel (PackageIdentity package, SourceRepository sourceRepository = null)
        {
            if (package == null)
                throw new ArgumentNullException (nameof (package));
            Package = package;
            DisplayName = package.GetFullName ();
            SourceRepository = sourceRepository;
        }
    }
}
