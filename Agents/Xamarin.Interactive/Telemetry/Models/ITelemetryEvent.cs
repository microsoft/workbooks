//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Xamarin.Interactive.Telemetry.Models
{
    interface ITelemetryEvent
    {
        Dictionary<string, string> GetProperties ();
    }
}