//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Xamarin.Interactive.SystemInformation
{
    interface ISoftwareEnvironment : IEnumerable<ISoftwareComponent>
    {
        string Name { get; }
    }
}