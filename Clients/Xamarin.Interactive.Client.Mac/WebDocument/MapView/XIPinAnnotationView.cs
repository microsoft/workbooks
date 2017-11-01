//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using CoreGraphics;
using CoreLocation;
using MapKit;

namespace Xamarin.Interactive.Client.Mac.WebDocument.MapView
{
    /// <summary>
	/// MKPinAnnotationView with an event to provide location mid-drag.
	/// </summary>
    sealed class XIPinAnnotationView : MKPinAnnotationView
    {
        public sealed class DragLocationChangedEventArgs : EventArgs
        {
            public CLLocationCoordinate2D DragLocation { get; set; }
        }

        public event EventHandler<DragLocationChangedEventArgs> DragLocationChanged;

        readonly MKMapView mapView;

        public XIPinAnnotationView (
            MKMapView mapView,
            IMKAnnotation annotation,
            string reuseIdentifier)
            : base (annotation, reuseIdentifier)
        {
            this.mapView = mapView;
        }

        // The only way to know the location of an annotation view mid-drag is to override setFrame.
        public override CGRect Frame {
            get {
                return base.Frame;
            }
            set {
                base.Frame = value;

                var center = new CGPoint (
                    value.GetMidX () - CenterOffset.X,
                    value.GetMidY () - CenterOffset.Y);
                var location = mapView.ConvertPoint (center, Superview);

                DragLocationChanged?.Invoke (this, new DragLocationChangedEventArgs {
                    DragLocation = location
                });
            }
        }
    }
}