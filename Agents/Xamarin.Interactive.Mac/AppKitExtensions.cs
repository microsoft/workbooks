//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Foundation;
using CoreGraphics;
using AppKit;

namespace Xamarin.Interactive.Mac
{
    static class AppKitExtensions
    {
        static NSData AsEncodedBitmapData (NSImage image, NSBitmapImageFileType fileType)
        {
            if (image == null)
                return null;

            image.LockFocus ();
            var rect = new CGRect (0, 0, image.Size.Width, image.Size.Height);
            var rep = new NSBitmapImageRep (rect);
            image.UnlockFocus ();
            return rep.RepresentationUsingTypeProperties (fileType, null);
        }

        public static NSData AsPNG (this NSImage image)
        {
            return AsEncodedBitmapData (image, NSBitmapImageFileType.Png);
        }

        public static NSData AsJPEG (this NSImage image)
        {
            return AsEncodedBitmapData (image, NSBitmapImageFileType.Jpeg);
        }
    }
}