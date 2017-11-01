//
// PackageSourceViewModel.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using NuGet.Protocol.Core.Types;

namespace Xamarin.Interactive.NuGet
{
    sealed class PackageSourceViewModel
    {
        public SourceRepository SourceRepository { get; }

        public string DisplayName { get; }

        public PackageSourceViewModel (SourceRepository repo)
        {
            if (repo == null)
                throw new ArgumentNullException (nameof (repo));

            SourceRepository = repo;
            DisplayName = repo.ToString ();
        }
    }
}
