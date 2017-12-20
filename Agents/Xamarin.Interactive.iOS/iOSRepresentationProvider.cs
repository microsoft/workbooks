//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;

using MapKit;
using UIKit;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.iOS
{
    sealed class iOSRepresentationProvider : UnifiedRepresentationProvider
    {
        public iOSRepresentationProvider ()
        {
            RegisterHandler<UIImage> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<UIColor> (uicolor => {
                nfloat r, g, b, a;
                uicolor.GetRGBA (out r, out g, out b, out a);
                return new Representation (new Color (r, g, b, a), true);
            });
        }

        public override bool HasSensibleEnumerator (IEnumerable enumerable)
        {
            // lots of UIKit types implement IEnumerable yet do not
            // actually enumerate anything...
            var type = enumerable.GetType ();

            // Arrays always have a sensible enumerator.
            if (type.IsArray)
                return true;

            while (type != null) {
                switch (type.Namespace) {
                case "UIKit":
                    return false;
                }

                type = type.BaseType;
            }

            return true;
        }

        public override bool TryConvertFromRepresentation (
            IRepresentedType representedType,
            object [] representations,
            out object represented)
        {
            represented = null;

            if (TryFindMatchingRepresentation<UIColor, Color> (
                representedType,
                representations,
                out var color)) {
                represented = new UIColor (
                    (nfloat)color.Red,
                    (nfloat)color.Green,
                    (nfloat)color.Blue,
                    (nfloat)color.Alpha);
                return true;
            }

            if (TryFindMatchingRepresentation<MKPolyline, GeoPolyline> (
                representedType,
                representations,
                out var polyline)) {
                represented = polyline.ToMKPolyline ();
                return true;
            }

            return base.TryConvertFromRepresentation (
                representedType,
                representations,
                out represented);
        }
    }
}