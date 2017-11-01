//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Xamarin.Interactive.Client.Windows.Views
{
    partial class ViewHighlightOverlayWindow : Window
    {
        public ViewHighlightOverlayWindow ()
        {
            InitializeComponent ();
        }

        public void HighlightRect (Rect rect)
        {
            var highlightRect = new Rectangle {
                Stroke = new SolidColorBrush (Colors.Tomato),
                Width = rect.Width,
                Height = rect.Height,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection {2, 2},
            };

            Canvas.SetLeft (highlightRect, rect.X - Left);
            Canvas.SetTop (highlightRect, rect.Y - Top);

            canvas.Children.Add (highlightRect);
        }

        public void Clear ()
        {
            canvas.Children.Clear ();
        }
    }
}
