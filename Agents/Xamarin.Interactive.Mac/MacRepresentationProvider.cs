//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using CoreGraphics;
using CoreText;
using Foundation;
using MapKit;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.Mac
{
    sealed class MacRepresentationProvider : UnifiedRepresentationProvider
    {
        const string TAG = nameof (MacRepresentationProvider);

        public MacRepresentationProvider ()
        {
            RegisterHandler<CGFont> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<CGPath> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<CGLineCap> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<CGLineJoin> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<CGBitmapContext> (GraphicsExtensions.RemoteRepresentation);

            RegisterHandler<CTFont> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<CTStringAttributes> (GraphicsExtensions.RemoteRepresentation);

            RegisterHandler<NSImage> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<NSAttributedString> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<NSFont> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<NSBezierPath> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<NSLineCapStyle> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<NSLineJoinStyle> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<NSColor> (nscolor => {
                try {
                    var calibratedRgb = nscolor?.UsingColorSpace (NSColorSpace.CalibratedRGB);
                    if (calibratedRgb != null) {
                        nfloat r, g, b, a;
                        calibratedRgb.GetRgba (out r, out g, out b, out a);
                        return new Representation (new Color (r, g, b, a), true);
                    }
                } catch (Exception e) {
                    Log.Error (TAG, e);
                }

                return new Color (0, 0, 0);
            });
        }

        public override bool TryConvertFromRepresentation (
            IRepresentedType representedType,
            object [] representations,
            out object represented)
        {
            represented = null;

            if (TryConvertRepresentation<NSColor, Color> (
                representedType,
                representations,
                out represented,
                color => NSColor.FromRgba (
                    (nfloat)color.Red,
                    (nfloat)color.Green,
                    (nfloat)color.Blue,
                    (nfloat)color.Alpha)))
                return true;

            if (TryConvertRepresentation<MKPolyline, GeoPolyline> (
                representedType,
                representations,
                out represented,
                polyline => polyline.ToMKPolyline ()))
                return true;

            return base.TryConvertFromRepresentation (
                representedType,
                representations,
                out represented);
        }
    }
}