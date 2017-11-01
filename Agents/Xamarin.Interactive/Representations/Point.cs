//
// Point.cs
//
// Author:
//   Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright 2014-2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    public sealed class Point : IRepresentationObject
    {
        public Point (double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        void ISerializableObject.Serialize (ObjectSerializer serializer)
        {
            throw new NotImplementedException ();
        }
    }
}