//
// AssemblyDependency.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class AssemblyDependency
    {
        public FilePath Location { get; }
        public byte [] Data { get; }

        public AssemblyDependency (FilePath location, byte [] data = null)
        {
            if (location == null)
                throw new ArgumentNullException (nameof (location));

            Location = location;
            Data = data;
        }
    }
}