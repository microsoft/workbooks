//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Telemetry
{
    interface IEvent
    {
        string Key { get; }
        DateTime Timestamp { get; }
    }
}