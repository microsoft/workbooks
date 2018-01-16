//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

using Xamarin.Interactive.Telemetry.Models;

namespace Xamarin.Interactive
{
    static class TelemetryEventExtensions
    {
        static readonly BlockingCollection<ITelemetryEvent> pendingEvents
            = new BlockingCollection<ITelemetryEvent> ();

        public static void Post (this ITelemetryEvent evnt, Telemetry.Client client = null)
        {
            if (client != null) {
                client.Post (evnt);
                return;
            }

            client = ClientApp.SharedInstance?.Telemetry;

            if (client == null) {
                pendingEvents.Add (evnt);
                return;
            }

            pendingEvents.CompleteAdding ();

            while (pendingEvents.TryTake (out var pendingEvent))
                client.Post (pendingEvent);

            client.Post (evnt);
        }
    }
}