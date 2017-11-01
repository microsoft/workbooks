//
// GeoLocation.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
	[Serializable]
	public sealed class GeoLocation : IRepresentationObject
	{
		public GeoLocation (
			double latitude,
			double longitude,
			double? altitude = null,
			double? horizontalAccuracy = null,
			double? verticalAccuracy = null,
			double? speed = null,
			double? bearing = null,
			DateTime timestamp = default (DateTime))
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

		void ISerializableObject.Serialize (ObjectSerializer serializer)
		{
			throw new NotImplementedException ();
		}
	}
}