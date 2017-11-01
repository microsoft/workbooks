//
// WpfEvaluationContextGlobalObject.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using System;
using System.Windows;
using System.Windows.Media.Imaging;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Wpf
{
    public sealed class WpfEvaluationContextGlobalObject : EvaluationContextGlobalObject
    {
        internal WpfEvaluationContextGlobalObject (Agent agent) : base (agent)
        {
        }

        [InteractiveHelp (Description = "Return a screenshot of the given view")]
        public static BitmapSource Capture (FrameworkElement view)
        {
            if (view == null)
                throw new ArgumentNullException (nameof(view));

            var width = (int) view.ActualWidth;
            var height = (int) view.ActualHeight;

            if (width <= 0 || height <= 0)
                return null;

            var bmp = new RenderTargetBitmap (
                width,
                height,
                96, // TODO: Handle other DPI settings
                96,
                System.Windows.Media.PixelFormats.Default);

            var dv = new System.Windows.Media.DrawingVisual ();
            using (var dc = dv.RenderOpen ()) {
                dc.DrawRectangle (
                    new System.Windows.Media.VisualBrush (view),
                    null,
                    new Rect (0, 0, width, height));
            }
            bmp.Render (dv);

            return bmp;
        }
    }
}