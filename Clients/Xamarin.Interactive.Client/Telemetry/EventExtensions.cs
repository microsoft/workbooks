//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Xamarin.Interactive
{
    static class TelemetryEventExtensions
    {
        static readonly BlockingCollection<Telemetry.IDataEvent> pendingEvents
            = new BlockingCollection<Telemetry.IDataEvent> ();

        public static void Post (this Telemetry.IDataEvent evnt, Telemetry.Client client = null)
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

        public static void Post (this Telemetry.IEvent evnt, Telemetry.Client client = null)
            => Post (new Telemetry.Event (evnt.Key, evnt.Timestamp), client);
    }
}