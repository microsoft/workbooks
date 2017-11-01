//
// InspectViewMaterial.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.

using System;

using AppKit;
using CoreGraphics;
using Foundation;

using Xamarin.Interactive;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Mac.ViewInspector
{
    sealed class InspectViewMaterial : NSImage
    {
        const string TAG = nameof (InspectViewMaterial);

        public static InspectViewMaterial Create (InspectView inspectView, bool renderImage = true)
        {
            if (inspectView == null)
                throw new ArgumentNullException (nameof (inspectView));

            if (Math.Abs (inspectView.Width) < Single.Epsilon ||
                Math.Abs (inspectView.Height) < Single.Epsilon)
                return null;

            if (renderImage && inspectView.BestCapturedImage != null) {
                try {
                    NativeExceptionHandler.Trap ();
                    return new InspectViewMaterial (inspectView.BestCapturedImage);
                } catch (Exception e) {
                    Log.Error (TAG, $"Exception creating NSImage from byte[] " +
                        $"(length={inspectView.CapturedImage.Length})", e);
                } finally {
                    NativeExceptionHandler.Release ();
                }
            }

            return new InspectViewMaterial (inspectView.Width, inspectView.Height);
        }

        InspectViewMaterial (byte [] imageData) : base (NSData.FromArray (imageData))
        {
        }

        InspectViewMaterial (double width, double height) : base (new CGSize (width, height))
        {
        }
    }
}
