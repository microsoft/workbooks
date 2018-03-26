// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [Serializable]
    public struct AssemblyDependency
    {
        public FilePath Location { get; }
        public byte [] Data { get; }

        internal AssemblyDependency (FilePath location, byte [] data = null)
        {
            if (location == null)
                throw new ArgumentNullException (nameof (location));

            Location = location;
            Data = data;
        }
    }
}