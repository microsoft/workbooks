//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.Telemetry.Models
{
    sealed class AgentSession : ITelemetryEvent
    {
        public Guid AgentSessionId { get; set; }
        public AgentSessionKind Kind { get; set; }
        public string Flavor { get; set; }
        public string ExternalTelemetrySessionId { get; set; }

        Dictionary<string, string> ITelemetryEvent.GetProperties ()
        {
            var dict = new Dictionary<string, string> ();

            if (AgentSessionId != Guid.Empty)
                dict.Add (nameof (AgentSessionId), AgentSessionId.ToString ());
            dict.Add (nameof (Kind), Kind.ToString ());
            if (!string.IsNullOrEmpty (Flavor))
                dict.Add (nameof (Flavor), Flavor);
            if (!string.IsNullOrEmpty (ExternalTelemetrySessionId))
                dict.Add (nameof (ExternalTelemetrySessionId), ExternalTelemetrySessionId);

            return dict;
        }
    }
}