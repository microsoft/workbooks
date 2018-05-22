// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [JsonObject]
    public struct AssemblyDependency
    {
        public string Name { get; }
        public FilePath Location { get; }
        public byte [] Data { get; }

        [JsonConstructor]
        internal AssemblyDependency (string name, FilePath location, byte [] data = null)
        {
            if (location == null)
                throw new ArgumentNullException (nameof (location));

            Name = name;
            Location = location;
            Data = data;
        }
    }
}