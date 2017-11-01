//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Numerics;

namespace Xamarin.Interactive.Camera
{
    public static class VectorUtilities
    {
        public static float Clamp (double v, double min, double max)
            => (float)Math.Max (Math.Min (v, max), min);

        public static Vector3 ClampComponents (Vector3 v, Vector3 min, Vector3 max)
            => new Vector3 (
                Clamp (v.X, min.X, max.X),
                Clamp (v.Y, min.Y, max.Y),
                Clamp (v.Z, min.Z, max.Z));

        public static Vector3 ClampComponents (Vector3 v, double min, double max)
            => new Vector3 (
                Clamp (v.X, min, max),
                Clamp (v.Y, min, max),
                Clamp (v.Z, min, max));

        public static void ToAxisAngle (Quaternion q, out Vector3 axis, out float angle)
        {
            angle = (float)(2 * Math.Acos (q.W));
            var scale  = (float)Math.Sqrt (1.0 - q.W * q.W);
            if (scale > .0001f)
                axis = new Vector3 (q.X, q.Y, q.Z) / scale;
            else
                axis = new Vector3 (0, 0, 1);
        }
    }
}