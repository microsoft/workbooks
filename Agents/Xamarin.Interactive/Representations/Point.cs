//
// Author:
//   Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    public sealed class Point
    {
        [JsonConstructor]
        public Point (double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }
    }
}