//
// WebDependency.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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