//
// AppKitExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014 Xamarin Inc. All rights reserved.

// FIXME: integrate these into Xamarin.Mac

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