//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Reflection
{
    abstract class ExternalDependency
    {
        public FilePath Location { get; }

        protected ExternalDependency (FilePath location)
        {
            Location = location;
        }
    }
}