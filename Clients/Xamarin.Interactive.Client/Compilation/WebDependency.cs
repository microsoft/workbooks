//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Reflection;

namespace Xamarin.Interactive.Compilation
{
    sealed class WebDependency : ExternalDependency
    {
        public WebDependency (FilePath location) : base (location)
        {
        }
    }
}