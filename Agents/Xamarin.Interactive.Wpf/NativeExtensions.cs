//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

using Xamarin.Interactive.Inspection;
using XIR = Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Wpf
{
    static class NativeExtensions
    {
        public static XIR.Image RemoteRepresentation (this BitmapSource source)
        {
            var encoder = new PngBitmapEncoder ();
            var memStream = new MemoryStream ();
            encoder.Frames.Add (BitmapFrame.Create (source));
            encoder.Save (memStream);

            return new XIR.Image (
                XIR.ImageFormat.Png,
                memStream.GetBuffer (),
                (int)source.Width,
                (int)source.Height);
        }

        public static ViewVisibility ToViewVisibility (this Visibility state)
        {
            switch (state) {
            case Visibility.Visible:
                return ViewVisibility.Visible;
            case Visibility.Hidden:
                return ViewVisibility.Hidden;
            case Visibility.Collapsed:
                return ViewVisibility.Collapsed;
            default:
                throw new ArgumentOutOfRangeException (
                    nameof (state),
                    state,
                    "Don't know how to convert given ViewState to ViewVisibility.");
            }
        }
    }
}
