//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    public sealed class GeoLocation : IRepresentationObject
    {
        [JsonConstructor]
        public GeoLocation (
            double latitude,
            double longitude,
            double? altitude = null,
            double? horizontalAccuracy = null,
            double? verticalAccuracy = null,
            double? speed = null,
            double? bearing = null,
            DateTime timestamp = default)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            HorizontalAccuracy = horizontalAccuracy;
            VerticalAccuracy = verticalAccuracy;
            Speed = speed;
            Bearing = bearing;
            Timestamp = timestamp;
        }

        public double Latitude { get; }
        public double Longitude { get; }
        public double? VerticalAccuracy { get; }
        public double? HorizontalAccuracy { get; }
        public double? Altitude { get; }
        public double? Speed { get; }
        public double? Bearing { get; }
        public DateTime Timestamp { get; }
    }
}