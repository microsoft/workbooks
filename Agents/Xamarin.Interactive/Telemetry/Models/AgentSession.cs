//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Telemetry.Models
{
    sealed class AgentSession : ITelemetryEvent
    {
        public Guid AgentSessionId { get; set; }
        public Guid AppSessionId { get; set; }
        public AppSession AppSession { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public AgentSessionKind Kind { get; set; }
        public string Flavor { get; set; }
    }
}