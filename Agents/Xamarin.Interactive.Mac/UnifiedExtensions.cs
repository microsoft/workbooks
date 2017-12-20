//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

using CoreAnimation;
using CoreGraphics;
using CoreLocation;
using MapKit;

using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Unified
{
    static class LocationExtensions
    {
        public static GeoLocation RemoteRepresentation (this CLLocationCoordinate2D coordinate)
        {
            if (!coordinate.IsValid ())
                return null;

            return new GeoLocation (coordinate.Latitude, coordinate.Longitude);
        }

        public static GeoLocation RemoteRepresentation (this CLLocation location)
        {
            // Check for invalid coordinates
            if (location.HorizontalAccuracy < 0)
                return null;

            return new GeoLocation (
                latitude: location.Coordinate.Latitude,
                longitude: location.Coordinate.Longitude,
                altitude: location.VerticalAccuracy >= 0 ? location.Altitude : default (double?),
                horizontalAccuracy: location.HorizontalAccuracy,
                verticalAccuracy: location.VerticalAccuracy,
                speed: location.Speed >= 0 ? location.Speed : default (double?),
                bearing: location.Course >= 0 ? location.Course : default (double?),
                timestamp: (DateTime)location.Timestamp);
        }

        public static CLLocationCoordinate2D ToCoordinate (this GeoLocation location)
            => new CLLocationCoordinate2D (location.Latitude, location.Longitude);

        public static GeoLocation RemoteRepresentation (this MKMapPoint point)
            => MKMapPoint.ToCoordinate (point).RemoteRepresentation ();

        public static GeoPolyline RemoteRepresentation (this MKPolyline polyline)
            => new GeoPolyline (polyline.Points.Select (RemoteRepresentation).ToArray ());

        public static CLLocationCoordinate2D[] ToCoordinates (this GeoPolyline polyline)
            => polyline.Points.Select (ToCoordinate).ToArray ();

        public static MKPolyline ToMKPolyline (this GeoPolyline polyline)
            => MKPolyline.FromCoordinates (polyline.ToCoordinates ());
    }

    static class CoreGraphicsExtensions
    {
        public static Size RemoteRepresentation (this CGSize size)
            => new Size (size.Width, size.Height);

        public static Point RemoteRepresentation (this CGPoint point)
            => new Point (point.X, point.Y);

        public static Rectangle RemoteRepresentation (this CGRect rectangle)
            => new Rectangle (rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    static class CoreAnimationExtensions
    {
        public static CATransform3D Prepend (this CATransform3D a, CATransform3D b) =>
            b.Concat (a);

        public static CATransform3D GetLocalTransform (this CALayer layer)
        {
            return CATransform3D.Identity
                .Translate (
                    layer.Position.X,
                    layer.Position.Y,
                    layer.ZPosition)
                .Prepend (layer.Transform)
                .Translate (
                    -layer.AnchorPoint.X * layer.Bounds.Width,
                    -layer.AnchorPoint.Y * layer.Bounds.Height,
                    -layer.AnchorPointZ);
        }

        public static CATransform3D GetChildTransform (this CALayer layer)
        {
            var childTransform = layer.SublayerTransform;

            if (childTransform.IsIdentity)
                return childTransform;

            return CATransform3D.Identity
                .Translate (
                    layer.AnchorPoint.X * layer.Bounds.Width,
                    layer.AnchorPoint.Y * layer.Bounds.Height,
                    layer.AnchorPointZ)
                .Prepend (childTransform)
                .Translate (
                    -layer.AnchorPoint.X * layer.Bounds.Width,
                    -layer.AnchorPoint.Y * layer.Bounds.Height,
                    -layer.AnchorPointZ);
        }

        public static CATransform3D TransformToAncestor (this CALayer fromLayer, CALayer toLayer)
        {
            var transform = CATransform3D.Identity;

            CALayer current = fromLayer;
            while (current != toLayer) {
                transform = transform.Concat (current.GetLocalTransform ());

                current = current.SuperLayer;
                if (current == null)
                    break;

                transform = transform.Concat (current.GetChildTransform ());
            }
            return transform;
        }

        public static ViewTransform ToViewTransform (this CATransform3D transform) =>
            new ViewTransform {
                M11 = transform.m11,
                M12 = transform.m12,
                M13 = transform.m13,
                M14 = transform.m14,
                M21 = transform.m21,
                M22 = transform.m22,
                M23 = transform.m23,
                M24 = transform.m24,
                M31 = transform.m31,
                M32 = transform.m32,
                M33 = transform.m33,
                M34 = transform.m34,
                OffsetX = transform.m41,
                OffsetY = transform.m42,
                OffsetZ = transform.m43,
                M44 = transform.m44
            };
    }
}