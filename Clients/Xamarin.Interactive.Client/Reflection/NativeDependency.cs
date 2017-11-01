//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Reflection
{
    class NativeDependency : ExternalDependency
    {
        public string Name { get; }

        public NativeDependency (string name, FilePath location) : base (location)
        {
            Name = name;
        }
    }
}