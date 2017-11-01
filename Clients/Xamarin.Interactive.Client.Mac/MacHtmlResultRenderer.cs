//
// HtmlResultRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014 Xamarin Inc.

using System;

using AppKit;
using Foundation;
using CoreGraphics;
using CoreText;

using XIR = Xamarin.Interactive.Remoting;

using Xamarin.Inspector.Repl;

namespace Xamarin.Interactive.Client.Mac
{
	sealed class MacHtmlResultRenderer : HtmlResultRenderer
	{

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
		// static NSColor brush = NSColor.FromDeviceRgba (102f / 255f, 239f / 255f, 127f / 255f, 127f / 255f);
		static NSColor pen = NSColor.FromDeviceRgba (0.345f, 0.658f, 0.368f, 1);

		// static string LabelFontNameRegular = "Menlo";
		static string LabelFontNameBold = "Menlo-Bold";

		// static NSFont Font = NSFont.UserFixedPitchFontOfSize (12f);
		// static NSFont LabelFont = NSFont.FromFontName(LabelFontNameRegular, NSFont.LabelFontSize);
		static NSFont LabelFontBold = NSFont.FromFontName(LabelFontNameBold, NSFont.LabelFontSize);

		static NSColor BackgroundColor = NSColor.White;

		/*public override string Render (object obj)
		{
			var point = obj as XIR.Point;
			if (point != null)
				return RenderPoint (point);

			var rectangle = obj as XIR.Rect;
			if (rectangle != null)
				return RenderRectangle (rectangle);

			var size = obj as XIR.Size;
			if (size != null)
				return RenderSize (size);

			return base.Render (obj);
		}*/

		static CTLine GetLabel (string label, out CGSize bounds)
		{
			return GetLabel (label, pen.CGColor, out bounds);
		}

		static CTLine GetSubLabel (string label, out CGSize bounds)
		{
			return GetLabel (label, NSColor.DarkGray.CGColor, out bounds);
		}

		static CTLine GetLabel (string label, CGColor foregroundColor, out CGSize bounds)
		{
			// I seriously have no idea what this is other than 8388608/1024/1024=8 -abock
			const int undocumentedKennethConstantOnlyHeUnderstands = 8388608;

			var typesetter = new CTTypesetter (
				new NSAttributedString (
					label,
					new CTStringAttributes {
						Font = new CTFont (LabelFontBold.FontName, LabelFontBold.PointSize),
						ForegroundColor = foregroundColor
					}.Dictionary
				)
			);

			var lineRange = new NSRange (0, typesetter.SuggestLineBreak (0,
				undocumentedKennethConstantOnlyHeUnderstands));
			var line = typesetter.GetLine (lineRange);

			nfloat ascent;
			nfloat descent;
			nfloat leading;
			var lineWidth = line.GetTypographicBounds (out ascent, out descent, out leading);

			bounds = new CGSize (
				// +1 matches best to CTFramesetter's behavior
				(nfloat)Math.Ceiling (ascent + descent + leading + 1),
				(nfloat)lineWidth
			);

			return line;
		}

		static string RenderPoint (XIR.Point point)
		{

			var base64 = string.Empty;

			var pointSize = new CGSize (8, 8);
			var rectangle = new CGRect (0, 0, 100, 100);

			var measure = CGSize.Empty;

			var line = GetLabel (string.Format ("({0:0.########}, {1:0.###########})", point.X, point.Y), out measure);

			if (measure.Width > rectangle.Width)
				rectangle.Size = new CGSize (measure.Width, measure.Width);

			int width = (int)rectangle.Width;
			int height = (int)rectangle.Height;

			var bytesPerRow = 4 * width;

			using (var context = new CGBitmapContext (
				IntPtr.Zero, width, height,
				8, bytesPerRow, CGColorSpace.CreateDeviceRGB (),
				CGImageAlphaInfo.PremultipliedFirst))
			{

				context.SetFillColor (BackgroundColor.CGColor);
				context.FillRect (rectangle);


				context.SetFillColor (pen.CGColor);
				var centerX = rectangle.GetMidX () - pointSize.Width / 2;
				var centerY = rectangle.GetMidY () - pointSize.Height / 2;
				context.FillEllipseInRect (new CGRect (centerX, centerY, pointSize.Width, pointSize.Height));

				context.ConcatCTM (context.GetCTM().Invert());
				var matrix = new CGAffineTransform (
					1, 0, 0, -1, 0, height);

				context.ConcatCTM (matrix);
				var textMatrix = new CGAffineTransform (
					1, 0, 0, -1, 0, 0);

				context.TextMatrix = textMatrix;

				context.TextPosition = new CGPoint (rectangle.GetMidX () - measure.Width / 2, rectangle.GetMidY () - (measure.Height * 2));

				line.Draw(context);
				line.Dispose ();

				line = GetSubLabel ("x, y", out measure);
				context.TextPosition = new CGPoint (rectangle.GetMidX () - measure.Width / 2, rectangle.GetMidY () - (measure.Height));

				line.Draw(context);
				line.Dispose ();

				var bitmap = new NSBitmapImageRep (context.ToImage());

				var data = bitmap.RepresentationUsingTypeProperties (NSBitmapImageFileType.Png);
				base64 = data.GetBase64EncodedString (NSDataBase64EncodingOptions.None);

			}

			return String.Format (
				"<figure>" +
					"<figcaption>" +
						"Point: " +
						"<span class='var'>X</span> = <span class='value'>{0:0.########}</span>, " +
						"<span class='var'>Y</span> = <span class='value'>{1:0.########}</span>" +
					"</figcaption>" +
					"<img width='{2}' height='{3}' src='data:image/png;base64,{4}' />" +
				"</figure>",
				point.X, point.Y,
				(int)rectangle.Width,
				(int)rectangle.Height,
				base64
			);
		}

		static string RenderRectangle(XIR.Rect rectangle)
		{
			var base64 = string.Empty;

			var size = new CGSize (rectangle.Width, rectangle.Height);
			var origin = new CGPoint (rectangle.X, rectangle.Y);

			// We want the absolute values of the size
			var workSize = new CGSize(Math.Abs (size.Width), Math.Abs (size.Height));

			// This is our scale factor for output
			var dstSize = new CGSize (100, 100);

			// Define our Height label variables
			var numHeightLabelBounds = CGSize.Empty;
			var heightLabelBounds = CGSize.Empty;
			var heightBounds = CGSize.Empty;

			// Obtain our label lines and bounding boxes of the labels
			var numHeightLine = GetLabel (string.Format ("{0:0.########}", size.Height), out numHeightLabelBounds);
			var heightLine = GetSubLabel ("Height", out heightLabelBounds);
			heightBounds.Width = NMath.Max (numHeightLabelBounds.Width, heightLabelBounds.Width);
			heightBounds.Height = NMath.Max (numHeightLabelBounds.Height, heightLabelBounds.Height);


			// Define our Width label variables
			var numWidthLabelBounds = CGSize.Empty;
			var widthLabelBounds = CGSize.Empty;
			var widthBounds = CGSize.Empty;

			// Obtain our label lines and bound boxes of the labels
			var numWidthLine = GetLabel (string.Format ("{0:0.########}", size.Width), out numWidthLabelBounds);
			var widthLine = GetSubLabel ("Width", out widthLabelBounds);
			widthBounds.Width = NMath.Max (numWidthLabelBounds.Width, widthLabelBounds.Width);
			widthBounds.Height = NMath.Max (numWidthLabelBounds.Height, widthLabelBounds.Height);


			// Define our Width label variables
			var originLabelBounds = CGSize.Empty;
			var xyLabelBounds = CGSize.Empty;
			var originBounds = CGSize.Empty;

			// Obtain our label lines and bound boxes of the labels
			var originLine = GetLabel (string.Format ("({0:0.########}, {1:0.########})", origin.X, origin.Y), out originLabelBounds);
			var xyLine = GetSubLabel ("x, y", out xyLabelBounds);
			originBounds.Width = NMath.Max (originLabelBounds.Width, xyLabelBounds.Width);
			originBounds.Height = NMath.Max (originLabelBounds.Height, xyLabelBounds.Height);

			// Calculate our scale based on our destination size
			var ratio = 1f;

			if (!workSize.IsEmpty)
			{
				if (workSize.Width > workSize.Height) {
					ratio = (float)workSize.Height / (float)workSize.Width;
					dstSize.Height = (int)(dstSize.Height * ratio);
				} else {
					ratio = (float)workSize.Width / (float)workSize.Height;
					dstSize.Width = (int)(dstSize.Width * ratio);
				}

				// Make sure we at least have something to draw if the values are very small
				dstSize.Width = NMath.Max (dstSize.Width, 4f);
				dstSize.Height = NMath.Max (dstSize.Height, 4f);
			}
			else
			{
				dstSize = CGSize.Empty;
			}


			// Define graphic element sizes and offsets
			var lineWidth = 2;
			var capSize = 8f;
			var vCapIndent = 3f;
			var separationSpace = 2f;
			var dotSize = new CGSize (6, 6);

			var extraBoundingSpaceWidth = (widthBounds.Width + separationSpace) * 2;
			var extraBoundingSpaceHeight = (heightBounds.Height + separationSpace) * 2;

			int width = (int)(dstSize.Width + lineWidth + capSize + vCapIndent + extraBoundingSpaceWidth);
			int height = (int)(dstSize.Height + lineWidth + capSize + extraBoundingSpaceHeight + originBounds.Height * 2);

			var bytesPerRow = 4 * width;

			using (var context = new CGBitmapContext (
				IntPtr.Zero, width, height,
				8, bytesPerRow, CGColorSpace.CreateDeviceRGB (),
				CGImageAlphaInfo.PremultipliedFirst))
			{

				// Clear the context with our background color
				context.SetFillColor (BackgroundColor.CGColor);
				context.FillRect (new CGRect(0,0,width,height));

				// Setup our matrices so our 0,0 is top left corner.  Just makes it easier to layout
				context.ConcatCTM (context.GetCTM().Invert());
				var matrix = new CGAffineTransform (
					1, 0, 0, -1, 0, height);

				context.ConcatCTM (matrix);

				context.SetStrokeColor (pen.CGColor);
				context.SetFillColor (pen.CGColor);
				context.SetLineWidth (lineWidth);

				context.SaveState ();

				// We need to offset the drawing of our size segment rulers leaving room for labels
				var xOffSet = heightBounds.Width;
				var yOffset = ((height +  originBounds.Height * 2) - extraBoundingSpaceHeight) / 2f - dstSize.Height / 2f;

				context.TranslateCTM (xOffSet, yOffset);

				context.AddEllipseInRect (new CGRect (vCapIndent - dotSize.Width / 2f, vCapIndent - dotSize.Height / 2f, dotSize.Width, dotSize.Height));
				context.FillPath ();

				context.AddRect (new CGRect (vCapIndent, vCapIndent, dstSize.Width, dstSize.Height));

				context.StrokePath ();

				context.RestoreState ();

				// Setup our text matrix
				var textMatrix = new CGAffineTransform (
					1, 0, 0, -1, 0, 0);

				context.TextMatrix = textMatrix;

				// Draw the Origin labels
				context.TextPosition = new CGPoint ((xOffSet + vCapIndent) - originLabelBounds.Width / 2, originLabelBounds.Height);
				originLine.Draw (context);

				context.TextPosition = new CGPoint ((xOffSet + vCapIndent) - xyLabelBounds.Width / 2, originLabelBounds.Height * 2);
				xyLine.Draw (context);


				// Draw the Height label
				var heightCenter = yOffset + ((dstSize.Height / 2) + ((vCapIndent + lineWidth) * 2));

				context.TextPosition = new CGPoint (heightBounds.Width / 2 - numHeightLabelBounds.Width / 2, heightCenter - heightBounds.Height / 2f);
				numHeightLine.Draw (context);

				context.TextPosition = new CGPoint (heightBounds.Width / 2 - heightLabelBounds.Width / 2, heightCenter + heightBounds.Height / 2f);
				heightLine.Draw (context);


				// Draw the Width label
				var widthOffsetX = heightBounds.Width + ((dstSize.Width / 2) - ((lineWidth + vCapIndent) * 2));//    xOffSet - vCapIndent - lineWidth + dstSize.Width / 2f;
				context.TextPosition = new CGPoint (widthOffsetX + (widthBounds.Width / 2 - numWidthLabelBounds.Width / 2), height - widthBounds.Height - 2f);
				numWidthLine.Draw (context);

				context.TextPosition = new CGPoint (widthOffsetX + (widthBounds.Width / 2 - widthLabelBounds.Width / 2), height - widthLabelBounds.Height / 2f);
				widthLine.Draw (context);

				// Get rid of our lines
				numHeightLine.Dispose ();
				heightLine.Dispose ();

				numWidthLine.Dispose ();
				widthLine.Dispose ();

				originLine.Dispose ();
				xyLine.Dispose ();

				// Convert to base64 for display
				var bitmap = new NSBitmapImageRep (context.ToImage());

				var data = bitmap.RepresentationUsingTypeProperties (NSBitmapImageFileType.Png);
				base64 = data.GetBase64EncodedString (NSDataBase64EncodingOptions.None);

			}

			return String.Format (
				"<figure>" +
					"<figcatpion>" +
						"Rectangle: " +
						"<span class='var'>X</span> = <span class='value'>{0:0.########}</span>, " +
						"<span class='var'>Y</span> = <span class='value'>{1:0.########}</span>, " +
						"<span class='var'>Width</span> = <span class='value'>{2:0.########}</span>, " +
						"<span class='var'>Height</span> = <span class='value'>{3:0.########}</span>" +
					"</figcaption>" +
					"<img width='{4}' height='{5}' src='data:image/png;base64,{6}' />" +
				"</figure>",
				origin.X, origin.Y,
				size.Width, size.Height,
				(int)width,
				(int)height,
				base64
			);
		}

		static string RenderSize(XIR.Size size)
		{

			var base64 = string.Empty;

			// We want the absolute values of the size
			var workSize = new CGSize(Math.Abs (size.Width), Math.Abs (size.Height));

			// This is our scale factor for output
			var dstSize = new CGSize (50, 50);

			// Define our Height label variables
			var numHeightLabelBounds = CGSize.Empty;
			var heightLabelBounds = CGSize.Empty;
			var heightBounds = CGSize.Empty;

			// Obtain our label lines and bounding boxes of the labels
			var numHeightLine = GetLabel (string.Format ("{0:0.########}", size.Height), out numHeightLabelBounds);
			var heightLine = GetSubLabel ("Height", out heightLabelBounds);
			heightBounds.Width = NMath.Max (numHeightLabelBounds.Width, heightLabelBounds.Width);
			heightBounds.Height = NMath.Max (numHeightLabelBounds.Height, heightLabelBounds.Height);


			// Define our Width label variables
			var numWidthLabelBounds = CGSize.Empty;
			var widthLabelBounds = CGSize.Empty;
			var widthBounds = CGSize.Empty;

			// Obtain our label lines and bound boxes of the labels
			var numWidthLine = GetLabel (string.Format ("{0:0.########}", size.Width), out numWidthLabelBounds);
			var widthLine = GetSubLabel ("Width", out widthLabelBounds);
			widthBounds.Width = NMath.Max (numWidthLabelBounds.Width, widthLabelBounds.Width);
			widthBounds.Height = NMath.Max (numWidthLabelBounds.Height, widthLabelBounds.Height);

			// Calculate our scale based on our destination size
			var ratio = 1f;
			if (workSize.Width > workSize.Height) {
				ratio = (float)workSize.Height / (float)workSize.Width;
				dstSize.Height = (int)(dstSize.Height * ratio);
			} else {
				ratio = (float)workSize.Width / (float)workSize.Height;
				dstSize.Width = (int)(dstSize.Width * ratio);
			}

			// Make sure we at least have something to draw if the values are very small
			dstSize.Width = NMath.Max (dstSize.Width, 4f);
			dstSize.Height = NMath.Max (dstSize.Height, 4f);

			// Define graphic element sizes and offsets
			const int lineWidth = 2;
			const float capSize = 8f;
			const float vCapIndent = 3f;
			const float separationSpace = 2f;

			var extraBoundingSpaceWidth = (widthBounds.Width + separationSpace) * 2;
			var extraBoundingSpaceHeight = (heightBounds.Height + separationSpace) * 2;

			int width = (int)(dstSize.Width + lineWidth + capSize + vCapIndent + extraBoundingSpaceWidth);
			int height = (int)(dstSize.Height + lineWidth + capSize + extraBoundingSpaceHeight);

			var bytesPerRow = 4 * width;

			using (var context = new CGBitmapContext (
				IntPtr.Zero, width, height,
				8, bytesPerRow, CGColorSpace.CreateDeviceRGB (),
				CGImageAlphaInfo.PremultipliedFirst))
			{

				// Clear the context with our background color
				context.SetFillColor (BackgroundColor.CGColor);
				context.FillRect (new CGRect(0,0,width,height));

				// Setup our matrices so our 0,0 is top left corner.  Just makes it easier to layout
				context.ConcatCTM (context.GetCTM().Invert());
				var matrix = new CGAffineTransform (
					1, 0, 0, -1, 0, height);

				context.ConcatCTM (matrix);

				context.SetStrokeColor (pen.CGColor);
				context.SetLineWidth (lineWidth);

				context.SaveState ();

				// We need to offset the drawing of our size segment rulers leaving room for labels
				var xOffSet = heightBounds.Width;
				var yOffset = (height - extraBoundingSpaceHeight) / 2f - dstSize.Height / 2f;

				context.TranslateCTM (xOffSet, yOffset);
			
				// Draw the Height segment ruler
				var vCapCenter = vCapIndent + (capSize / 2f);

				context.AddLines (new CGPoint[] { new CGPoint (vCapIndent, 1), new CGPoint (vCapIndent + capSize, 1),
					new CGPoint (vCapCenter, 1), new CGPoint (vCapCenter, dstSize.Height),
					new CGPoint (vCapIndent, dstSize.Height), new CGPoint (vCapIndent + capSize, dstSize.Height),
				});


				// Draw the Width segment ruler
				var hCapIndent = vCapIndent + capSize + separationSpace;
				var hCapOffsetY = dstSize.Height;
				var hCapCenter = hCapOffsetY + (capSize / 2f);
				context.AddLines (new CGPoint[] { new CGPoint (hCapIndent, hCapOffsetY), new CGPoint (hCapIndent, hCapOffsetY + capSize ),
					new CGPoint (hCapIndent, hCapCenter), new CGPoint (hCapIndent + dstSize.Width, hCapCenter),
					new CGPoint (hCapIndent + dstSize.Width, hCapOffsetY), new CGPoint (hCapIndent + dstSize.Width, hCapOffsetY + capSize),
				});
					
				context.StrokePath ();

				context.RestoreState ();

				// Setup our text matrix
				var textMatrix = new CGAffineTransform (
					1, 0, 0, -1, 0, 0);

				context.TextMatrix = textMatrix;


				// Draw the Height label
				context.TextPosition = new CGPoint (heightBounds.Width / 2 - numHeightLabelBounds.Width / 2, height / 2 - heightBounds.Height / 2);
				numHeightLine.Draw (context);

				context.TextPosition = new CGPoint (heightBounds.Width / 2 - heightLabelBounds.Width / 2, height / 2 + heightBounds.Height / 2);
				heightLine.Draw (context);


				// Draw the Width label
				var widthOffsetX = heightBounds.Width - separationSpace + dstSize.Width / 2;
				context.TextPosition = new CGPoint (widthOffsetX + (widthBounds.Width / 2 - numWidthLabelBounds.Width / 2), height - widthBounds.Height - 2);
				numWidthLine.Draw (context);

				context.TextPosition = new CGPoint (widthOffsetX + (widthBounds.Width / 2 - widthLabelBounds.Width / 2), height - widthLabelBounds.Height / 2);
				widthLine.Draw (context);

				// Get rid of our lines
				numHeightLine.Dispose ();
				heightLine.Dispose ();

				numWidthLine.Dispose ();
				widthLine.Dispose ();

				// Convert to base64 for display
				var bitmap = new NSBitmapImageRep (context.ToImage());

				var data = bitmap.RepresentationUsingTypeProperties (NSBitmapImageFileType.Png);
				base64 = data.GetBase64EncodedString (NSDataBase64EncodingOptions.None);

			}

			return String.Format ("" +
				"<figure>" +
					"<figcaption>" +
						"Size: " +
						"<span class='var'>Width</span> = <span class='value'>{0:0.########}</span>, " +
						"<span class='var'>Height</span> = <span class='value'>{1:0.########}</span>" +
					"</figcaption>" +
					"<img width='{2}' height='{3}' src='data:image/png;base64,{4}' />" +
				"</figure>",
				size.Width, size.Height,
				(int)width,
				(int)height,
				base64
			);

		}

	}
}