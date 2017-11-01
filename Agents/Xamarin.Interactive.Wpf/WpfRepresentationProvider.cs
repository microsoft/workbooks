//
// WpfRepresentationProvider.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//   Larry Ewing <lewing@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Linq;
using SWM = System.Windows.Media;
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

            if (representedType.ResolvedType == typeof (SWM.Color)) {
                var color = representations.OfType<XIR.Color> ().FirstOrDefault ();
                if (color != null) {
                    represented = SWM.Color.FromArgb (
                        (byte)(color.Alpha * 255),
                        (byte)(color.Red * 255),
                        (byte)(color.Green * 255),
                        (byte)(color.Blue * 255)
                    );
                    return true;
                }
            }

            return base.TryConvertFromRepresentation (
                representedType,
                representations,
                out represented);
        }
    }
}