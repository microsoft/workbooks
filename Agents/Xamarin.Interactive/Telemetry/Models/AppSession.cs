//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Telemetry.Models
{
    sealed class AppSession : ITelemetryEvent
    {
        public Guid AppSessionId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Version { get; set; }
        public string BuildHash { get; set; }
        public string UpdateChannel { get; set; }
        public OperatingSystem OperatingSystem { get; set; }
    }
}