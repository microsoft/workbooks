//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.Telemetry.Models
{
    sealed class AgentSession : ITelemetryEvent
    {
        public Guid AgentSessionId { get; set; }
        public ClientSessionKind ClientKind { get; set; }
        public string AgentFlavor { get; set; }
        public string ExternalTelemetrySessionId { get; set; }

        IEnumerable<KeyValuePair<string, string>> ITelemetryEvent.GetProperties ()
        {
            if (AgentSessionId != Guid.Empty)
                yield return new KeyValuePair<string, string> (
                    nameof (AgentSessionId),
                    AgentSessionId.ToString ());

            yield return new KeyValuePair<string, string> (
                nameof (ClientKind),
                ClientKind.ToString ());

            if (!string.IsNullOrEmpty (AgentFlavor))
                yield return new KeyValuePair<string, string> (
                    nameof (AgentFlavor),
                    AgentFlavor);
            
            if (!string.IsNullOrEmpty (ExternalTelemetrySessionId))
                yield return new KeyValuePair<string, string> (
                    nameof (ExternalTelemetrySessionId),
                    ExternalTelemetrySessionId);
        }
    }
}