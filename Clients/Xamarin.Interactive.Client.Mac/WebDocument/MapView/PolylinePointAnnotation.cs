//
// PolylinePointAnnotation.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

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