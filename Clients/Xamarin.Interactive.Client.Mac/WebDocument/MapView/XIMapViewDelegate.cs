//
// XIMapViewDelegate.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;
using Foundation;
using MapKit;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.Client.Mac.WebDocument.MapView
{
	sealed class XIMapViewDelegate : MKMapViewDelegate
	{
		bool isDragging;
		MKPolyline editPolyline;

		public override MKAnnotationView GetViewForAnnotation (MKMapView mapView, IMKAnnotation annotation)
		{
			var pointAnnotation = (PolylinePointAnnotation)annotation;

			var annotationView = new XIPinAnnotationView (mapView, annotation, "ximpav") {
				Draggable = pointAnnotation.Polyline?.CanWrite == true,
				AnimatesDrop = true,
				CanShowCallout = true,
			};

			annotationView.DragLocationChanged += (o, e) => {
				var value = annotationView.Frame;
				if (!isDragging || mapView?.Overlays == null)
					return;

				if (editPolyline != null)
					mapView.RemoveOverlay (editPolyline);

				var newPoints = pointAnnotation.Polyline.Value.ToCoordinates ();
				newPoints [pointAnnotation.Index] = e.DragLocation;

				editPolyline = MKPolyline.FromCoordinates (newPoints);
				mapView.AddOverlay (editPolyline);
			};

			return annotationView;
		}

		public override void ChangedDragState (
			MKMapView mapView,
			MKAnnotationView annotationView,
			MKAnnotationViewDragState newState,
			MKAnnotationViewDragState oldState)
		{
			if (newState == MKAnnotationViewDragState.Ending) {
				isDragging = false;

				if (mapView.Overlays != null)
					mapView.RemoveOverlays (mapView.Overlays);

				var annotation = (PolylinePointAnnotation)annotationView.Annotation;
				annotation.Polyline.Value.Points [annotation.Index] = new GeoLocation (
					annotation.Coordinate.Latitude,
					annotation.Coordinate.Longitude);
				var newPolyline = annotation.Polyline.Value.ToMKPolyline ();
				mapView.AddOverlay (newPolyline);

				annotation.Polyline.RaisePropertyChanged ();

				return;
			} else if (newState == MKAnnotationViewDragState.Starting
				|| newState == MKAnnotationViewDragState.Dragging)
				isDragging = true;
			else {
				isDragging = false;
				if (editPolyline != null)
					mapView.RemoveOverlay (editPolyline);
			}
		}

		public override MKOverlayRenderer OverlayRenderer (MKMapView mapView, IMKOverlay overlay)
		{
			var polyline = overlay as MKPolyline;
			if (polyline == null)
				return null;

			return new MKPolylineRenderer (polyline) {
				Alpha = (nfloat)0.6,
				LineWidth = isDragging ? 2 : 4,
				LineDashPattern = isDragging ? new [] { new NSNumber (2), new NSNumber (5) } : null,
				StrokeColor = isDragging ? NSColor.Blue : NSColor.Red,
			};
		}
	}
}