//
// ISoftwareEnvironment.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Xamarin.Interactive.SystemInformation
{
    interface ISoftwareEnvironment : IEnumerable<ISoftwareComponent>
    {
        string Name { get; }
    }
}