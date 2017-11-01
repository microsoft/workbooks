//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    public sealed class Color : IFallbackRepresentationObject, ISerializableObject, IEquatable<Color>
    {
        public ColorSpace ColorSpace { get; }

        public double Alpha { get; }

        public double Red { get; }
        public double Green { get; }
        public double Blue { get; }

        public Color (double red, double green, double blue, double alpha = 1)
        {
            ColorSpace = ColorSpace.Rgb;
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        void ISerializableObject.Serialize (ObjectSerializer serializer)
        {
            serializer.Property (nameof (ColorSpace), (int)ColorSpace);
            serializer.Property (nameof (Alpha), Alpha);
            serializer.Property (nameof (Red), Red);
            serializer.Property (nameof (Green), Green);
            serializer.Property (nameof (Blue), Blue);
        }

        public override int GetHashCode ()
            => Hash.Combine ((int)ColorSpace, Hash.Combine (Alpha, Red, Green, Blue));

        public override bool Equals (object obj)
            => Equals (obj as Color);

        public bool Equals (Color other) =>
            other != null &&
            other.ColorSpace == ColorSpace &&
            other.Alpha == Alpha &&
            other.Red == Red &&
            other.Green == Green &&
            other.Blue == Blue;
    }
}