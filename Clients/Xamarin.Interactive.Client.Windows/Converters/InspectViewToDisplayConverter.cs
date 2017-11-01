// InspectViewToDisplayConverter.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2016 Xamarin Inc.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Windows.Converters
{
    public class InspectViewToDisplayConverter : IValueConverter
    {
        public bool ShowBounds { get; set; }

        public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return String.Empty;

            var view = (InspectView) value;
            var boundsDisplay = String.Empty;

            if (ShowBounds) {
                var size = new Size (0, 0);
                if (false || view?.CapturedImage != null) {
                    var bitmap = new BitmapImage ();
                    bitmap.BeginInit ();

                    bitmap.StreamSource = new MemoryStream (view.CapturedImage);
                    bitmap.EndInit ();

                    size = new Size ((int) bitmap.Width, (int) bitmap.Height);
                }
                boundsDisplay = $"({view.X}, {view.Y}, {view.Width}, {view.Height}) - ({size.Width}, {size.Height})";
            }

            if (!String.IsNullOrEmpty (view.DisplayName))
                return view.DisplayName + boundsDisplay;

            var text = view.Type;

            var ofs = text.IndexOf ('.');
            if (ofs > 0) {
                switch (text.Substring (0, ofs)) {
                case "AppKit":
                case "SceneKit":
                case "WebKit":
                case "UIKit":
                    text = text.Substring (ofs + 1);
                    break;
                }
            }

            if (!String.IsNullOrEmpty (view.Description))
                text += $" — “{view.Description}”";

            text += boundsDisplay;
            return text;
        }

        public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException ();
        }
    }
}
