//
// PackageViewModel.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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
