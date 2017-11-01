//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive
{
    static class TelemetryEventExtensions
    {
        public static void Post (this Telemetry.IDataEvent evnt, Telemetry.Client client = null)
            => (client ?? ClientApp.SharedInstance.Telemetry).Post (evnt);

        public static void Post (this Telemetry.IEvent evnt, Telemetry.Client client = null)
            => Post (new Telemetry.Event (evnt.Key, evnt.Timestamp), client);
    }
}