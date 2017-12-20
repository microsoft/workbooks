//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Larry Ewing <lewing@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using SWM = System.Windows.Media;
using SW = System.Windows;

using System.Windows.Media.Imaging;

using XIR = Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Wpf
{
    sealed class WpfRepresentationProvider : XIR.AgentRepresentationProvider
    {
        public WpfRepresentationProvider()
        {
            RegisterHandler<SWM.Color> (c => new XIR.Representation (
                    new XIR.Color (
                        c.R / 255f,
                        c.G / 255f,
                        c.B / 255f,
                        c.A / 255f
                    ),
                    true));

            RegisterHandler<SW.Size> (s => new XIR.Representation (
                new XIR.Size (s.Width, s.Height), true));
            RegisterHandler<SW.Rect> (r => new XIR.Representation (
                new XIR.Rectangle (r.X, r.Y, r.Width, r.Height), true));
            RegisterHandler<SW.Point> (p => new XIR.Representation (
                new XIR.Point (p.X, p.Y), true));
            RegisterHandler<SW.Thickness> (t => new XIR.Representation (
                new XIR.Thickness (t.Left, t.Top, t.Right, t.Bottom), true));

            RegisterHandler<BitmapSource> (NativeExtensions.RemoteRepresentation);
        }

        public override object NormalizeRepresentation (object obj)
        {
            var image = obj as XIR.Image;
            if (image != null)
                return NormalizeImage (image);

            return null;
        }

        public static object NormalizeImage (XIR.Image image)
        {
            SWM.PixelFormat format;
            int components;
            byte [] buffer;

            switch (image.Format) {
            case XIR.ImageFormat.Bgr24:
                format = SWM.PixelFormats.Bgr24;
                components = 3;
                break;
            case XIR.ImageFormat.Rgb24:
                format = SWM.PixelFormats.Rgb24;
                components = 3;
                break;
            case XIR.ImageFormat.Rgba32:
                format = SWM.PixelFormats.Bgra32;
                components = 4;
                break;
            case XIR.ImageFormat.Bgra32:
                format = SWM.PixelFormats.Bgra32;
                components = 4;
                break;
            default:
                return null;
            }

            switch (image.Format) {
            case XIR.ImageFormat.Rgba32:
                buffer = new byte [image.Data.Length];
                Array.Copy (image.Data, buffer, buffer.Length);
                for (int i = 0; i < image.Data.Length; i += components) {
                    var b = buffer [i];
                    buffer [i] = buffer [i + 2];
                    buffer [i + 2] = b;
                }
                break;
            default:
                buffer = image.Data;
                break;
            }

            return BitmapSource.Create (
                image.Width, image.Height,
                96, 96,
                format,
                null,
                buffer,
                components * image.Width).RemoteRepresentation ();
        }

        public override bool TryConvertFromRepresentation (
            IRepresentedType representedType,
            object[] representations,
            out object represented)
        {
            represented = null;

            if (TryFindMatchingRepresentation<SW.Size, XIR.Size> (
                representedType,
                representations,
                out var size)) {
                represented = new SW.Size (size.Width, size.Height);
                return true;
            }

            if (TryFindMatchingRepresentation<SW.Rect, XIR.Rectangle> (
                representedType,
                representations,
                out var rectangle)) {
                represented = new SW.Rect (
                    rectangle.X,
                    rectangle.Y,
                    rectangle.Width,
                    rectangle.Height);
                return true;
            }

            if (TryFindMatchingRepresentation<SW.Point, XIR.Point> (
                representedType,
                representations,
                out var point)) {
                represented = new SW.Point (point.X, point.Y);
                return true;
            }

            if (TryFindMatchingRepresentation<SW.Thickness, XIR.Thickness> (
                representedType,
                representations,
                out var thickness)) {
                represented = new SW.Thickness (
                    thickness.Left,
                    thickness.Top,
                    thickness.Right,
                    thickness.Bottom);
                return true;
            }

            if (TryFindMatchingRepresentation<SWM.Color, XIR.Color> (
                representedType,
                representations,
                out var color)) {
                represented = SWM.Color.FromArgb (
                    (byte)(color.Alpha * 255),
                    (byte)(color.Red * 255),
                    (byte)(color.Green * 255),
                    (byte)(color.Blue * 255));
                return true;
            }

            return base.TryConvertFromRepresentation (
                representedType,
                representations,
                out represented);
        }
    }
}