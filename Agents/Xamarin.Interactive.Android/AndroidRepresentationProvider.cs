//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

using AG = Android.Graphics;
using AL = Android.Locations;

namespace Xamarin.Interactive.Android
{
    sealed class AndroidRepresentationProvider : AgentRepresentationProvider
    {
        public AndroidRepresentationProvider ()
        {
            RegisterHandler<AG.Bitmap> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<AG.Color> (agColor => new Representation (
                new Color (
                    agColor.R / 255f,
                    agColor.G / 255f,
                    agColor.B / 255f,
                    agColor.A / 255f),
                true));
            RegisterHandler<AG.Paint> (agPaint => {
                var cBitmap = AG.Bitmap.CreateBitmap (50, 50, AG.Bitmap.Config.Argb8888);
                try {
                    using (var canvas = new AG.Canvas (cBitmap)) {
                        canvas.DrawPaint (agPaint);
                        return cBitmap.RemoteRepresentation ();
                    }
                } finally {
                    cBitmap.Recycle ();
                }
            });

            RegisterHandler<AL.Location> (LocationExtensions.RemoteRepresentation);
        }

        public override bool TryConvertFromRepresentation (
            IRepresentedType representedType,
            object [] representations,
            out object represented)
        {
            if (TryConvertRepresentation<AG.Color, Color> (
                representedType,
                representations,
                out represented,
                color => new AG.Color (
                    r: (byte)(color.Red * 255),
                    g: (byte)(color.Green * 255),
                    b: (byte)(color.Blue * 255),
                    a: (byte)(color.Alpha * 255))))
                return true;

            return base.TryConvertFromRepresentation (
                representedType,
                representations,
                out represented);
        }

        public override object NormalizeRepresentation (object obj)
        {
            var image = obj as Image;
            if (image != null)
                return NormalizeImage (image);

            return null;
        }

        static Image NormalizeImage (Image input)
        {
            int bpp, r, g, b, a;
            uint mask;

            switch (input.Format) {
            case ImageFormat.Rgb24:
                bpp = 3;
                a = 0;
                r = 0;
                g = 1;
                b = 2;
                mask = 0xff000000;
                break;
            case ImageFormat.Bgr24:
                bpp = 3;
                a = 0;
                r = 2;
                g = 1;
                b = 0;
                mask = 0xff000000;
                break;
            case ImageFormat.Rgba32:
                bpp = 4;
                a = 3;
                r = 0;
                g = 1;
                b = 2;
                mask = 0x0;
                break;
            case ImageFormat.Bgra32:
                bpp = 4;
                a = 3;
                r = 2;
                g = 1;
                b = 0;
                mask = 0x0;
                break;
            default:
                return null;
            }

            var pixels = new int [input.Width * input.Height];
            var data = input.Data;
            unchecked {
                for (int dataPos = 0, pixelPos = 0; dataPos < data.Length; dataPos += bpp, pixelPos++) {
                    pixels [pixelPos] = (int) mask
                        | (data [dataPos + a] << 24)
                        | (data [dataPos + r] << 16)
                        | (data [dataPos + g] << 8)
                        | data [dataPos + b];
                }
            }

            var bitmap = AG.Bitmap.CreateBitmap (pixels, input.Width, input.Height, AG.Bitmap.Config.Argb8888);
            return new Image (ImageFormat.Png, bitmap.AsPNGBytes (), bitmap.Width, bitmap.Height);
        }
    }
}