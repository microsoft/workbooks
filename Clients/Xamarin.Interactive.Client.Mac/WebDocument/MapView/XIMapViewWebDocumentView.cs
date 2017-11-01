//
// XIMapViewWebDocumentView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;
using CoreLocation;
using Foundation;
using MapKit;
using WebKit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.Client.Mac.WebDocument.MapView
{
	[Register]
	sealed class XIMapViewWebDocumentView : XIWebDocumentView
	{
		readonly MKMapView mapView;

		protected override NSView ContentView {
			get { return mapView; }
		}

		public XIMapViewWebDocumentView ()
		{
			mapView = new MKMapView {
				ShowsZoomControls = true,
				Delegate = new XIMapViewDelegate (),
			};
		}

		public override NSView HitTest (CoreGraphics.CGPoint aPoint)
		{
			return ConvertRectToView (Bounds, null).Contains (aPoint)
				? mapView.HitTest (ConvertPointFromView (aPoint, Window.ContentView))
				: null;
		}

		protected override void DataSourceUpdated (WebDataSource oldDataSource, WebDataSource newDataSource)
		{
			if (mapView.Overlays != null)
				mapView.RemoveOverlays (mapView.Overlays);

			var representation = (XIMapViewWebDocumentRepresentation)newDataSource?.Representation;
			if (representation == null || !representation.LocationIsValid)
				return;

			if (representation.Polyline == null) {
				var coordinate = representation.Location.ToCoordinate ();
				AddLocationAnnotation (coordinate);
				mapView.Region = MKCoordinateRegion.FromDistance (coordinate, 15000, 15000);
			} else {
				var coords = representation.Polyline.Value.ToCoordinates ();
				for (var i = 0; i < coords.Length; i++)
					AddLocationAnnotation (
						coords [i],
						representation.Polyline,
						i);

				var mkpolyline = MKPolyline.FromCoordinates (coords);
				mapView.AddOverlay (mkpolyline);
				mapView.SetVisibleMapRect (
					mkpolyline.BoundingMapRect,
					new NSEdgeInsets (45, 35, 35, 35),
					false);
			}
		}

		void AddLocationAnnotation (
			CLLocationCoordinate2D coordinate,
			ChangeableWrapper<GeoPolyline> polyline = null,
			int index = 0)
		{
			var locationStr = ToLatLonString (
				coordinate.Latitude,
				coordinate.Longitude);
			var annotation = new PolylinePointAnnotation {
				Title = locationStr,
				Coordinate = coordinate,
				Polyline = polyline,
				Index = index,
			};

			mapView.AddAnnotation (annotation);
			mapView.SelectAnnotation (annotation, animated: true);
		}

		static string ToLatLonString (double lat, double lon)
		{
			return
				ToDegreesMinutesSeconds (lat) +
				(lat >= 0 ? "N " : "S ") +
				ToDegreesMinutesSeconds (lon) +
				// Google marks the direction of 0,0 as E; doing the same
				(lon >= 0 ? "E" : "W");
		}

		static string ToDegreesMinutesSeconds (double coordinate)
		{
			coordinate = Math.Abs (coordinate);

			var degrees = Math.Floor (coordinate);

			var doubleMinutes = 60 * (coordinate - degrees);
			var minutes = Math.Floor (doubleMinutes);

			var seconds = 60 * (doubleMinutes - minutes);

			return $"{degrees}°{minutes}'{seconds:F1}''";
		}
	}
}