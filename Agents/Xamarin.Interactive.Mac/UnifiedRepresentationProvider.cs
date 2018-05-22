//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using CoreGraphics;
using CoreLocation;
using MapKit;
using SceneKit;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using CoreAnimation;

#if IOS
using UIKit;

using Xamarin.Interactive.iOS;
#elif MAC
using Xamarin.Interactive.Mac;
#endif

namespace Xamarin.Interactive.Unified
{
    class UnifiedRepresentationProvider : AgentRepresentationProvider
    {
        public UnifiedRepresentationProvider ()
        {
            RegisterHandler<CGImage> (GraphicsExtensions.RemoteRepresentation);
            RegisterHandler<CGColor> (cgcolor => {
                var components = cgcolor.Components;
                // In Monochrome colorspace, there are only two components: the black level and the
                // alpha. Monochrome and RGB are handled fine below. Others at least won't crash, if
                // they ever turn up.
                return new Representation (
                    new Color (
                        components [0],
                        components [Math.Max (components.Length - 3, 0)],
                        components [Math.Max (components.Length - 2, 0)],
                        components [components.Length - 1]),
                    true);
            });

            RegisterHandler<CGRect> (o => new Representation (o.RemoteRepresentation (), true));
            RegisterHandler<CGPoint> (o => new Representation (o.RemoteRepresentation (), true));
            RegisterHandler<CGSize> (o => new Representation (o.RemoteRepresentation (), true));

            RegisterHandler<CLLocation> (LocationExtensions.RemoteRepresentation);
            RegisterHandler<CLLocationCoordinate2D> (LocationExtensions.RemoteRepresentation);
            RegisterHandler<MKMapPoint> (LocationExtensions.RemoteRepresentation);
            RegisterHandler<MKPolyline> (o => new Representation (o.RemoteRepresentation (), true));
        }

        public override bool ShouldReflect (object obj)
        {
            if (obj is nint || obj is nuint || obj is nfloat)
                return false;

            return true;
        }

        public override bool ShouldReadMemberValue (IRepresentedMemberInfo representedMemberInfo, object obj)
        {
            // these are known to crash the host application in native (C) code, but not
            // via a trappable Objective-C exception. Skip these to avoid crashes.
            // https://bugzilla.xamarin.com/show_bug.cgi?id=45340
            if (obj is SCNView) {
                switch (representedMemberInfo.Name) {
                case nameof (SCNView.AudioEnvironmentNode):
                case nameof (SCNView.AudioListener):
                    return false;
                }
            }

            // in native code CALayer.Contents is backed by an id not specific
            // type.  The binding does insufficient type checking when trying
            // to access it as a CGImage and can cause a native crash so we avoid
            // inspecting it here.
            if (obj is CALayer) {
                switch (representedMemberInfo.Name) {
                case nameof (CALayer.Contents):
                    return false;
                }
            }

#if IOS
            // These are known to mess with the rendering of the view on iOS
            if (obj is UIView) {
                switch (representedMemberInfo.Name) {
                case nameof (UIView.ViewForBaselineLayout):
                case nameof (UIView.ViewForFirstBaselineLayout):
                case nameof (UIView.ViewForLastBaselineLayout):
                    return false;
                }
            }
#endif

            return true;
        }

        public override bool TryConvertFromRepresentation (
            IRepresentedType representedType,
            object [] representations,
            out object represented)
        {
            represented = null;

            if (TryConvertRepresentation<CGColor, Color> (
                representedType,
                representations,
                out represented,
                c => new CGColor (
                    (nfloat)c.Red,
                    (nfloat)c.Green,
                    (nfloat)c.Blue,
                    (nfloat)c.Alpha)))
               return true;

            if (TryConvertRepresentation<CGSize, Size> (
                representedType,
                representations,
                out represented,
                s => new CGSize (s.Width, s.Height)))
                return true;

            if (TryConvertRepresentation<CGRect, Rectangle> (
                representedType,
                representations,
                out represented,
                r => new CGRect (r.X, r.Y, r.Width, r.Height)))
                return true;

            if (TryConvertRepresentation<CGPoint, Point> (
                representedType,
                representations,
                out represented,
                p => new CGPoint (x: p.X, y: p.Y)))
                return true;

            return base.TryConvertFromRepresentation (
                representedType,
                representations,
                out represented);
        }

        public override object NormalizeRepresentation (object obj)
        {
            switch (obj) {
            case Image image:
                return NormalizeImage (image);
            case nint nativeInt:
                return new WordSizedNumber (obj, WordSizedNumberFlags.Signed, (ulong)nativeInt);
            case nuint nativeUInt:
                return new WordSizedNumber (obj, WordSizedNumberFlags.None, nativeUInt);
            case nfloat nativeFloat:
                return new WordSizedNumber (
                    obj,
                    WordSizedNumberFlags.Real,
                    (ulong)BitConverter.DoubleToInt64Bits (nativeFloat));
            default:
                return null;
            }
        }

        static Image NormalizeImage (Image image)
        {
            const int bitsPerComponent = 8;
            int components;
            uint flags = 0;
            byte [] buffer;

            switch (image.Format) {
            case ImageFormat.Rgb24:
            case ImageFormat.Bgr24:
                components = 3;
                break;
            case ImageFormat.Bgra32:
                components = 4;
                flags = (uint)CGBitmapFlags.ByteOrder32Little | (uint)CGImageAlphaInfo.First;
                break;
            case ImageFormat.Rgba32:
                components = 4;
                flags = (uint)CGImageAlphaInfo.Last;
                break;
            default:
                return null;
            }

            switch (image.Format) {
            case ImageFormat.Bgr24:
                // FIXME: ugh, CGImage really does not want 24bpp in BGR order...
                // No CGBitmapFlags can convince it to do the channel swap unless
                // the buffer is 32bpp.
                buffer = new byte [image.Data.Length];
                Array.Copy (image.Data, buffer, buffer.Length);
                for (int i = 0; i < buffer.Length; i += components) {
                    var b = buffer [i];
                    buffer [i] = buffer [i + 2];
                    buffer [i + 2] = b;
                }
                break;
            default:
                buffer = image.Data;
                break;
            }

            using (NativeExceptionHandler.Trap ())
                return new CGImage (
                    image.Width,
                    image.Height,
                    bitsPerComponent,
                    bitsPerComponent * components,
                    image.Width * components,
                    CGColorSpace.CreateDeviceRGB (),
                    (CGBitmapFlags)flags,
                    new CGDataProvider (buffer),
                    null,
                    false,
                    CGColorRenderingIntent.AbsoluteColorimetric).RemoteRepresentation ();
        }
    }
}
