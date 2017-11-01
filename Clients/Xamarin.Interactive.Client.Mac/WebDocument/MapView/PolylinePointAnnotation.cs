//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MapKit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Client.Mac.WebDocument.MapView
{
    sealed class PolylinePointAnnotation : MKPointAnnotation
    {
        public int Index { get; set; }
        public ChangeableWrapper<GeoPolyline> Polyline { get; set; }
    }
}