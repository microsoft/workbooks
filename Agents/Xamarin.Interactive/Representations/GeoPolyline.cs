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
    public sealed class GeoPolyline : IRepresentationObject
    {
        public GeoPolyline (GeoLocation [] points)
        {
            if (points == null)
                throw new ArgumentNullException (nameof (points));

            if (points.Length < 1)
                throw new ArgumentOutOfRangeException (nameof (points), "must have at least one");

            Points = points;
        }

        public GeoLocation [] Points { get; }

        void ISerializableObject.Serialize (ObjectSerializer serializer)
        {
            throw new NotImplementedException ();
        }
    }
}