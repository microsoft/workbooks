//
// NativeExtenstions.cs
//
// Author:
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;
using Foundation;
using CoreGraphics;
using CoreText;

using XIR = Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Mac
{
    static class GraphicsExtensions
    {

        const string exampleString = "A quick brown fox jumped over the lazy fence";
        // Greyish Blue
        //		static NSColor pen = NSColor.FromDeviceRgba (195f/255f, 201f / 255f, 207f / 255f, 196f / 255f);
        //		static NSColor brush = NSColor.FromDeviceRgba (221f/255f, 221f/255f, 240f/255f, 127f/255f);

        // Pinkish
        //		static NSColor pen = NSColor.FromDeviceRgba (249f / 255f, 190f / 255f, 166f / 255f, 196f / 255f);
        //		static NSColor brush = NSColor.FromDeviceRgba (255f / 255f, 224f / 255f, 224f / 255f, 127f / 255f);

        // Bluish
        //		static NSColor brush = NSColor.FromDeviceRgba (215f / 255f, 232f / 255f, 255f / 255f, 255f / 255f);
        //		static NSColor pen = NSColor.FromDeviceRgba (30f / 255f, 108f / 255f, 255f / 255f, 255f / 255f);

        // Greenish
        static NSColor brush = NSColor.FromDeviceRgba (102f / 255f, 239f / 255f, 127f / 255f, 127f / 255f);
        static NSColor pen = NSColor.FromDeviceRgba (0, 51f / 255f, 0, 255f / 255f);


        public static CGImage Scale(this CGImage image, CGSize dstSize, bool keepAspect = true)
        {
            if (keepAspect)
            {
                var ratio = 1f;
                if (image.Width > image.Height) {
                    ratio = (float)image.Height / (float)image.Width;
                    dstSize.Height = (int)(dstSize.Height * ratio);
                } else {
                    ratio = (float)image.Width / (float)image.Height;
                    dstSize.Width = (int)(dstSize.Width * ratio);
                }
            }

            var width = (nint)dstSize.Width;
            var height = (nint)dstSize.Height;

            // Now draw our image
            var bytesPerRow = width * 4;

            using (var context = new CGBitmapContext (
                IntPtr.Zero, width, height,
                8, bytesPerRow, CGColorSpace.CreateDeviceRGB (),
                CGImageAlphaInfo.PremultipliedFirst))
            {
                var rect = CGRect.Empty;
                rect.Size = dstSize;
                context.DrawImage(rect, image);
                return context.ToImage ();
            }

        }

        public static NSBitmapImageRep Scale(this NSBitmapImageRep imageRep, CGSize dstSize, bool keepAspect = true)
        {
            var image = imageRep.CGImage;

            image = image.Scale (dstSize, keepAspect);
            return new NSBitmapImageRep (image);
        }

        #region AppKit and Foundation
        public static XIR.Image RemoteRepresentation (this NSLineJoinStyle obj)
        {

            // Customize the line cap style for the new object.
            var aPath = new NSBezierPath ();
            var lineWidth = 10;
            var sampleWidth = 50;

            // First we draw the presentation line
            aPath.LineWidth = lineWidth;
            aPath.MoveTo (new CGPoint (lineWidth, lineWidth));
            aPath.LineTo (new CGPoint (lineWidth + sampleWidth / 2, sampleWidth));
            aPath.LineTo (new CGPoint (lineWidth + sampleWidth, lineWidth));

            switch ((NSLineJoinStyle)obj) {
            case NSLineJoinStyle.Bevel:
                aPath.LineJoinStyle = NSLineJoinStyle.Bevel;
                break;
            case NSLineJoinStyle.Miter:
                aPath.LineJoinStyle = NSLineJoinStyle.Miter;
                break;
            case NSLineJoinStyle.Round:
                aPath.LineJoinStyle = NSLineJoinStyle.Round;
                break;
            }

            // let's make sure we leave a little room for the line width drawing as well by adding the lineWidth as well
            var width = aPath.ControlPointBounds.Right + lineWidth;
            var height = aPath.ControlPointBounds.Bottom + lineWidth;

            var nsimage = new NSImage (new CGSize (width, height));
            nsimage.LockFocus ();

            brush.Set ();
            aPath.Stroke ();

            // Second, we draw the inset line to demonstrate the bounds
            aPath.RemoveAllPoints ();
            aPath.LineWidth = 2;
            aPath.MoveTo (new CGPoint (lineWidth, lineWidth));
            aPath.LineTo (new CGPoint (lineWidth + sampleWidth / 2, sampleWidth));
            aPath.LineTo (new CGPoint (lineWidth + sampleWidth, lineWidth));

            pen.Set ();
            aPath.Stroke ();

            nsimage.UnlockFocus ();
            return nsimage.RemoteRepresentation ();
        }

        public static XIR.Image RemoteRepresentation (this NSLineCapStyle obj)
        {

            // Customize the line cap style for the new object.
            var aPath = new NSBezierPath ();
            var lineWidth = 16;
            var sampleWidth = 100;

            // First we draw the presentation line
            aPath.LineWidth = lineWidth;
            aPath.MoveTo (new CGPoint (lineWidth, lineWidth));
            aPath.LineTo (new CGPoint (lineWidth + sampleWidth, lineWidth));

            switch ((NSLineCapStyle)obj) {
            case NSLineCapStyle.Square:
                aPath.LineCapStyle = NSLineCapStyle.Square;
                break;
            case NSLineCapStyle.Butt:
                aPath.LineCapStyle = NSLineCapStyle.Butt;
                break;
            case NSLineCapStyle.Round:
                aPath.LineCapStyle = NSLineCapStyle.Round;
                break;
            }

            // let's make sure we leave a little room for the line width drawing as well by adding the lineWidth as well
            var width = aPath.ControlPointBounds.Right + lineWidth;
            var height = aPath.ControlPointBounds.Bottom + lineWidth;

            var nsimage = new NSImage (new CGSize (width, height));
            nsimage.LockFocus ();

            // We need to offset the image a little so it will not be cut off
            var transform = new NSAffineTransform ();
            transform.Translate (aPath.LineWidth / 2, aPath.LineWidth / 2);
            aPath.TransformUsingAffineTransform (transform);

            brush.Set ();
            aPath.Stroke ();

            // Second, we draw the inset line to demonstrate the bounds
            aPath.RemoveAllPoints ();
            lineWidth += lineWidth / 2;
            aPath.LineWidth = 2;
            aPath.MoveTo (new CGPoint (lineWidth, lineWidth));
            aPath.LineTo (new CGPoint (lineWidth + sampleWidth, lineWidth));

            pen.Set ();
            aPath.Stroke ();

            // Third, we draw the inset line endings which are two circles
            aPath.RemoveAllPoints ();
            var circleWidth = 2;
            aPath.LineWidth = circleWidth;
            aPath.LineCapStyle = NSLineCapStyle.Butt;
            aPath.AppendPathWithOvalInRect (new CGRect (lineWidth - (int)(circleWidth / 2), lineWidth - (int)(circleWidth / 2), circleWidth, circleWidth));
            aPath.AppendPathWithOvalInRect (new CGRect (lineWidth + sampleWidth - (int)(circleWidth / 2), lineWidth - (int)(circleWidth / 2), circleWidth, circleWidth));

            pen.Set ();
            aPath.Stroke ();
            nsimage.UnlockFocus ();
            return nsimage.RemoteRepresentation ();
        }

        public static XIR.Image RemoteRepresentation (this NSBezierPath nsbezierpath)
        {
            nfloat width = 1f;
            nfloat height = 1f;

            // We check here for the element count.  If it is zero an error is being thrown if you access ControlPointBounds.
            if (nsbezierpath.ElementCount > 0) {
                // let's make sure we leave a little room for the line width drawing as well by adding the LineWidth of the
                // bezier path.
                width = nsbezierpath.ControlPointBounds.Width + nsbezierpath.LineWidth * 2;
                height = nsbezierpath.ControlPointBounds.Height + nsbezierpath.LineWidth * 2;
            }
            else
            {
                return new NSImage (new CGSize (width, height)).RemoteRepresentation ();
            }

            var nsimage = new NSImage (new CGSize (width, height));

            nsimage.LockFocus ();
            var transform = new NSAffineTransform ();

            // We need to offset the image a little, specifically by the line width, so it will not be cut off
            var offsetXZero = -nsbezierpath.ControlPointBounds.X;
            var offsetYZero = -nsbezierpath.ControlPointBounds.Y;
            transform.Translate (offsetXZero + nsbezierpath.LineWidth / 2, offsetYZero + nsbezierpath.LineWidth / 2);
            nsbezierpath.TransformUsingAffineTransform (transform);

            brush.SetFill ();
            nsbezierpath.Fill ();

            pen.SetStroke ();
            nsbezierpath.Stroke ();

            nsimage.UnlockFocus ();

            return nsimage.RemoteRepresentation ();
        }

        public static XIR.Image RemoteRepresentation (this NSView nsview)
        {
            var bitmap = nsview.BitmapImageRepForCachingDisplayInRect (nsview.Bounds);
            if (bitmap == null)
                return null;
            nsview.CacheDisplay (nsview.Bounds, bitmap);
            return bitmap.RemoteRepresentation ();
        }

        public static XIR.Image RemoteRepresentation (this NSFont nsfont)
        {
            var atts = new CTStringAttributes ();
            atts.Font = new CTFont (nsfont.FontName, nsfont.PointSize);
            var attdString = new NSAttributedString(exampleString, atts.Dictionary);
            return RemoteRepresentation (attdString);
        }

        public static XIR.Image RemoteRepresentation (this NSAttributedString attributedString)
        {
            var typesetter = new CTTypesetter(attributedString);
            var measure = CGSize.Empty;

            var count = typesetter.SuggestLineBreak (0, 8388608);
            var line = typesetter.GetLine (new NSRange (0, count));

            // Create and initialize some values from the bounds.
            nfloat ascent;
            nfloat descent;
            nfloat leading;
            var lineWidth = line.GetTypographicBounds (out ascent, out descent, out leading);

            measure.Height += (float)Math.Ceiling (ascent + descent + leading + 1); // +1 matches best to CTFramesetter's behavior  
            measure.Width = (float)lineWidth;

            var width = (int)measure.Width > 0 ? (int)measure.Width : 200;
            var height = (int)measure.Height > 0 ? (int)measure.Height : 200;

            var bytesPerRow = width * 4;

            using (var context = new CGBitmapContext (
                IntPtr.Zero, width, height,
                8, bytesPerRow, CGColorSpace.CreateDeviceRGB (),
                CGImageAlphaInfo.PremultipliedFirst))
            {
                context.ConcatCTM (context.GetCTM().Invert());
                var matrix = new CGAffineTransform (
                    1, 0, 0, -1, 0, height);

                context.ConcatCTM (matrix);
                var textMatrix = new CGAffineTransform (
                    1, 0, 0, -1, 0, ascent);

                context.TextMatrix = textMatrix;
                line.Draw(context);
                line.Dispose ();

                return RemoteRepresentation (context);
            }
        }

        public static XIR.Image RemoteRepresentation (this NSBitmapImageRep bitmapImageRep)
        {
            return RemoteRepresentation (bitmapImageRep.CGImage);
        }

        public static XIR.Image RemoteRepresentation (this NSImage image)
        {
            var png = image?.AsPNG ();
            if (png == null)
                return null;

            return new XIR.Image (
                XIR.ImageFormat.Png,
                png.ToArray (),
                (int)image.Size.Width,
                (int)image.Size.Height);
        }

        #endregion


        #region CoreGraphics

        public static XIR.Image RemoteRepresentation (this CGBitmapContext context)
        {
            return RemoteRepresentation (context.ToImage ());
        }

        public static XIR.Image RemoteRepresentation (this CGImage image)
        {
            return RemoteRepresentation (image, image.Width, image.Height);
        }

        static XIR.Image RemoteRepresentation(CGImage image, nint width, nint height)
        {
            NSBitmapImageRep bitmap;

            if (width != image.Width || height != image.Height)
            {
                bitmap = new NSBitmapImageRep (image.Scale (new CGSize (width, height)));

            }
            else
            {
                bitmap = new NSBitmapImageRep (image);
            }

            var data = bitmap.RepresentationUsingTypeProperties (NSBitmapImageFileType.Png);
            return new XIR.Image (
                XIR.ImageFormat.Png,
                data.ToArray (),
                (int)width,
                (int)height);
        }

        public static XIR.Image RemoteRepresentation (this CGLineCap obj)
        {
            var aPath = new CGPath ();
            var lineWidth = 10;
            var sampleWidth = 50;

            aPath.MoveToPoint (new CGPoint (lineWidth, lineWidth));
            aPath.AddLineToPoint (new CGPoint (lineWidth + sampleWidth, lineWidth));

            // let's make sure we leave a little room for the line width drawing as well by adding the lineWidth as well
            var width = (int)aPath.PathBoundingBox.Right + lineWidth;
            var height = (int)aPath.PathBoundingBox.Bottom + lineWidth;

            var bytesPerRow = width * 4;

            using (var context = new CGBitmapContext (
                IntPtr.Zero, width, height,
                8, bytesPerRow, CGColorSpace.CreateDeviceRGB (),
                CGImageAlphaInfo.PremultipliedFirst)) {
                context.SaveState ();
                context.SetStrokeColor (new CGColor (0, 0, 0));
                context.SetLineWidth (lineWidth);
                context.AddPath (aPath);
                switch ((CGLineCap)obj) {
                case CGLineCap.Square:
                    context.SetLineCap (CGLineCap.Square);
                    break;
                case CGLineCap.Butt:
                    context.SetLineCap (CGLineCap.Butt);
                    break;
                case CGLineCap.Round:
                    context.SetLineCap (CGLineCap.Round);
                    break;
                }

                context.DrawPath (CGPathDrawingMode.Stroke);

                context.RestoreState ();

                // Second, we draw the inset line to demonstrate the bounds
                aPath = new CGPath ();
                aPath.MoveToPoint (new CGPoint (lineWidth, lineWidth));
                aPath.AddLineToPoint (new CGPoint (lineWidth + sampleWidth, lineWidth));
                context.SetLineCap (CGLineCap.Butt);
                context.SetStrokeColor (NSColor.White.CGColor);
                context.SetLineWidth (1);

                context.SaveState ();

                context.AddPath (aPath);
                context.DrawPath (CGPathDrawingMode.Stroke);

                context.RestoreState ();


                // Third, we draw the inset line endings which are two circles
                var circleWidth = 2;
                aPath = new CGPath ();
                aPath.AddEllipseInRect (new CGRect (lineWidth - (int)(circleWidth / 2), lineWidth - (int)(circleWidth / 2), circleWidth, circleWidth));
                aPath.AddEllipseInRect (new CGRect (lineWidth + sampleWidth - (int)(circleWidth / 2), lineWidth - (int)(circleWidth / 2), circleWidth, circleWidth));
                context.SetLineWidth (circleWidth);
                context.SetStrokeColor (NSColor.White.CGColor);
                context.AddPath (aPath);
                context.DrawPath (CGPathDrawingMode.Stroke);

                return RemoteRepresentation (context);
            }
        }

        public static XIR.Image RemoteRepresentation (this CGLineJoin obj)
        {
            // Customize the line cap style for the new object.
            var aPath = new CGPath ();
            var lineWidth = 10;
            var sampleWidth = 50;

            // First we draw the presentation line
            aPath.MoveToPoint (new CGPoint (lineWidth, lineWidth));
            aPath.AddLineToPoint (new CGPoint (lineWidth + sampleWidth / 2, sampleWidth));
            aPath.AddLineToPoint (new CGPoint (lineWidth + sampleWidth, lineWidth));

            // let's make sure we leave a little room for the line width drawing as well by adding the lineWidth as well
            var width = (int)aPath.PathBoundingBox.Right + lineWidth;
            var height = (int)aPath.PathBoundingBox.Bottom + lineWidth;

            var bytesPerRow = width * 4;

            using (var context = new CGBitmapContext (
                IntPtr.Zero, width, height,
                8, bytesPerRow, CGColorSpace.CreateDeviceRGB (),
                CGImageAlphaInfo.PremultipliedFirst)) {
                context.SaveState ();
                context.SetStrokeColor (NSColor.Black.CGColor);
                context.SetLineWidth (lineWidth);
                context.AddPath (aPath);
                switch ((CGLineJoin)obj) {
                case CGLineJoin.Bevel:
                    context.SetLineJoin (CGLineJoin.Bevel);
                    break;
                case CGLineJoin.Miter:
                    context.SetLineJoin (CGLineJoin.Miter);
                    break;
                case CGLineJoin.Round:
                    context.SetLineJoin (CGLineJoin.Round);
                    break;
                }

                context.DrawPath (CGPathDrawingMode.Stroke);

                context.RestoreState ();

                aPath = new CGPath ();

                aPath.MoveToPoint (new CGPoint (lineWidth, lineWidth));
                aPath.AddLineToPoint (new CGPoint (lineWidth + sampleWidth / 2, sampleWidth));
                aPath.AddLineToPoint (new CGPoint (lineWidth + sampleWidth, lineWidth));

                context.SetStrokeColor (NSColor.White.CGColor);
                context.SetLineWidth (2);
                context.AddPath (aPath);

                context.DrawPath (CGPathDrawingMode.Stroke);

                return context.RemoteRepresentation ();
            }
        }

        public static XIR.Image RemoteRepresentation (this CGPath cgPath)
        {

            // We add just a little padding to keep from clipping the drawings that lie on the bounds.
            var width = (int)cgPath.PathBoundingBox.Width + 4 > 0 ? (int)cgPath.PathBoundingBox.Width + 4 : 200;
            var height = (int)cgPath.PathBoundingBox.Height + 4 > 0 ? (int)cgPath.PathBoundingBox.Height + 4 : 200;

            var bytesPerRow = width * 4;

            // We need to offset the image to keep from clipping the drawing.
            var offsetXZero = -cgPath.PathBoundingBox.X;
            var offsetYZero = -cgPath.PathBoundingBox.Y;

            // Create a transform to offset our drawing.
            var transform = CGAffineTransform.MakeIdentity ();
            transform.Translate (offsetXZero + 1, offsetYZero + 1);

            using (var context = new CGBitmapContext (
                IntPtr.Zero, width, height,
                8, bytesPerRow, CGColorSpace.CreateDeviceRGB (),
                CGImageAlphaInfo.PremultipliedFirst))
            {
                // Make sure we offset our drawing to keep it form clipping
                context.ConcatCTM (transform);

                context.SaveState ();
                context.SetFillColor (brush.CGColor);
                context.AddPath (cgPath);
                context.FillPath ();
                context.RestoreState ();

                context.SetStrokeColor (pen.CGColor);
                context.SetLineWidth (1f);
                context.AddPath (cgPath);
                context.DrawPath (CGPathDrawingMode.Stroke);

                return context.RemoteRepresentation ();
            }
        }

        public static XIR.Image RemoteRepresentation (this CGFont cgfont)
        {
            var atts = new CTStringAttributes ();
            atts.Font = new CTFont (cgfont, 12, null);
            var attdString = new NSAttributedString (exampleString, atts.Dictionary);
            return attdString.RemoteRepresentation ();
        }
        #endregion

        #region CoreText
        public static XIR.Image RemoteRepresentation (this CTStringAttributes ctstringattributes)
        {
            var attdString = new NSAttributedString (exampleString, ctstringattributes.Dictionary);
            return attdString.RemoteRepresentation ();
        }

        public static XIR.Image RemoteRepresentation (this CTFont ctfont)
        {
            var atts = new CTStringAttributes ();
            atts.Font = ctfont;
            var attdString = new NSAttributedString (exampleString, atts.Dictionary);
            return attdString.RemoteRepresentation ();
        }

        #endregion

        public static string Base64Data (this XIR.Image image)
        {
            return Convert.ToBase64String (image.Data);
        }
    }
}

