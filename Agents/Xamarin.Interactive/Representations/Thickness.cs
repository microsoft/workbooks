//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    class Thickness : IRepresentationObject
    {
        public Thickness (double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public double Left { get; }
        public double Top { get; }
        public double Right { get; }
        public double Bottom { get; }

        void ISerializableObject.Serialize (ObjectSerializer serializer)
            => throw new NotImplementedException ();
    }
}
