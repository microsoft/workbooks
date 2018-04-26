//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    public sealed class GeoPolyline
    {
        public IReadOnlyList<GeoLocation> Points { get; }

        [JsonConstructor]
        public GeoPolyline (IReadOnlyList<GeoLocation> points)
        {
            if (points == null)
                throw new ArgumentNullException (nameof (points));

            if (points.Count < 1)
                throw new ArgumentOutOfRangeException (nameof (points), "must have at least one");

            Points = points;
        }
    }
}