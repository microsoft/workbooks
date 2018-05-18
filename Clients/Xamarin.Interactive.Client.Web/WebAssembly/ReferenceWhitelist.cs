//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.Web.WebAssembly
{
    public class ReferenceWhitelist
    {
        ImmutableHashSet<FilePath> whitelistedFiles = ImmutableHashSet.Create<FilePath> ();

        public void Add (FilePath path)
            => whitelistedFiles = whitelistedFiles.Add (path);

        public void Remove (FilePath path)
            => whitelistedFiles = whitelistedFiles.Remove (path);

        public void Clear ()
            => whitelistedFiles = whitelistedFiles.Clear ();

        public bool Contains (FilePath path)
            => whitelistedFiles.Contains (path);
    }
}