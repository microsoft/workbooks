//
// NativeExtensions.cs
//
// Author:
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.IO;
using SD = System.Drawing;

using AG = Android.Graphics;
using AL = Android.Locations;
using Android.Views;

using Xamarin.Interactive.Inspection;
using XIR = Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Android
{
    static class NativeExtensions
    {
        public static View[] Subviews (this View parent)
        {
            var viewGroup = parent as ViewGroup;

            if (viewGroup == null)
                return null;

            var subViews = new View[viewGroup.ChildCount];
            for(int i = 0; i < viewGroup.ChildCount; i++)
            {
                subViews [i] = viewGroup.GetChildAt (i);
            }

            return subViews;
        }

        public static ViewVisibility ToViewVisibility (this ViewStates state)
        {
            switch (state) {
            case ViewStates.Gone:
                // Android's Gone means "Not shown, not considered for layout."
                return ViewVisibility.Collapsed;
            case ViewStates.Invisible:
                // Invisibile means "Not shown, considered for layout."
                return ViewVisibility.Hidden;
            case ViewStates.Visible:
                return ViewVisibility.Visible;
            default:
                throw new ArgumentOutOfRangeException (
                    nameof (state),
                    state,
                    "Don't know how to convert given ViewState to ViewVisibility.");
            }
        }

        public static string TrimStart(this string source, string trimString)
        {
            string trimmed = source;
            if (source.StartsWith (trimString))
                trimmed = trimmed.Substring (trimString.Length);

            return trimmed;
        }

        public static string TrimId(this string source)
        {
            var idOffset = source.IndexOf (":id/");
            if (idOffset > 0)
                return source.Substring (idOffset + 1);

            return source;
        }

    }

    static class GraphicsExtensions
    {


        public static AG.Bitmap Scale(this AG.Bitmap bitemap, SD.Size dstSize, bool keepAspect = true)
        {
            if (keepAspect)
            {
                var ratio = 1f;
                if (bitemap.Width > bitemap.Height) {
                    ratio = (float)bitemap.Height / (float)bitemap.Width;
                    dstSize.Height = (int)(dstSize.Height * ratio);
                } else {
                    ratio = (float)bitemap.Width / (float)bitemap.Height;
                    dstSize.Width = (int)(dstSize.Width * ratio);
                }
            }

            var width = dstSize.Width;
            var height = dstSize.Height;

            var scaled = AG.Bitmap.CreateScaledBitmap (bitemap, width, height, true);

            return scaled;
        }

        #region System Drawing Graphic Primitives

        public static XIR.Point RemoteRepresentation (this SD.PointF point)
            => new XIR.Point (point.X, point.Y);

        public static XIR.Rectangle RemoteRepresentation (this SD.RectangleF rectangle)
            => new XIR.Rectangle (
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                rectangle.Height);

        public static XIR.Size RemoteRepresentation (this SD.SizeF size)
            => new XIR.Size (size.Width, size.Height);

        #endregion

        #region Android Graphics Graphic Primitives

        public static XIR.Point RemoteRepresentation (this AG.Point point)
            => new XIR.Point (point.X, point.Y);

        public static XIR.Point RemoteRepresentation (this AG.PointF point)
            => new XIR.Point (point.X, point.Y);

        public static XIR.Rectangle RemoteRepresentation (this AG.Rect rectangle)
            => new XIR.Rectangle (
                rectangle.Left,
                rectangle.Top,
                rectangle.Width (),
                rectangle.Height ());

        public static XIR.Rectangle RemoteRepresentation (this AG.RectF rectangle)
            => new XIR.Rectangle (
                rectangle.Left,
                rectangle.Top,
                rectangle.Width (),
                rectangle.Height ());

        #endregion

        #region Android App and View

        public static XIR.Image RemoteRepresentation (this View view)
        {
            var enabled = view.DrawingCacheEnabled;
            view.DrawingCacheEnabled = true;
            var bitmap = AG.Bitmap.CreateBitmap(view.DrawingCache);
            view.DrawingCacheEnabled = enabled;
            return bitmap.RemoteRepresentation ();
        }

        #endregion

        public static XIR.Image RemoteRepresentation(this AG.Bitmap bitmap)
        {
            return RemoteRepresentation (bitmap, bitmap.Width, bitmap.Height);
        }

        static XIR.Image RemoteRepresentation(AG.Bitmap bitmap, int width, int height)
        {
            if (width != bitmap.Width || height != bitmap.Height)
                bitmap = bitmap.Scale (new System.Drawing.Size (width, height));

            return new XIR.Image (
                XIR.ImageFormat.Png,
                bitmap.ToByteArray (),
                width,
                height);
        }

        public static byte [] ToByteArray (this AG.Bitmap bitmap)
        {
            using (var mem = new MemoryStream ()) {
                bitmap.Compress (AG.Bitmap.CompressFormat.Png, 100, mem);
                return mem.ToArray ();
            }
        }

        public static string ToBase64String(this AG.Bitmap bitmap)
        {
            return Convert.ToBase64String (bitmap.ToByteArray ());
        }
    }

    public static class LocationExtensions
    {
        public static XIR.GeoLocation RemoteRepresentation (this AL.Location location)
            => new XIR.GeoLocation (
                latitude: location.Latitude,
                longitude: location.Longitude,
                altitude: location.HasAltitude ? location.Altitude : default (double?),
                horizontalAccuracy: location.HasAccuracy ? location.Accuracy : default (double?),
                speed: location.HasSpeed ? location.Speed : default (double?),
                bearing: location.HasBearing ? location.Bearing : default (double?),
                timestamp: new DateTime (1970, 1, 1).AddSeconds (location.Time));
    }
}