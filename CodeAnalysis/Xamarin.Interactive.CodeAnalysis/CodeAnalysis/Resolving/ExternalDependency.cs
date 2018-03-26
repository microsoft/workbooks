//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    public class ExternalDependency
    {
        public FilePath Location { get; }

        public ExternalDependency (FilePath location)
            => Location = location;
    }
}