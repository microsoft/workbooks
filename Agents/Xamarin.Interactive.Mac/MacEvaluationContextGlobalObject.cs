//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

using ObjCRuntime;
using AppKit;
using CoreGraphics;

using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.Mac
{
    public sealed class MacEvaluationContextGlobalObject : UnifiedEvaluationContextGlobalObject
    {
        readonly MacAgent agent;

        internal MacEvaluationContextGlobalObject (MacAgent agent) : base (agent)
            => this.agent = agent;

        [InteractiveHelp (Description = "Quick access to NSApplication.SharedApplication")]
        public static NSApplication App {
            get { return NSApplication.SharedApplication; }
        }

        [InteractiveHelp (Description = "Quick access to NSApplication.SharedApplication.Delegate")]
        public static INSApplicationDelegate AppDelegate {
            get { return NSApplication.SharedApplication.Delegate; }
        }

        [InteractiveHelp (Description = "Quick access to NSApplication.SharedApplication.MainWindow, " +
            "or the first window for the application if there is no MainWindow")]
        public NSWindow MainWindow => agent.GetMainWindow ();

        [DllImport (Constants.CoreGraphicsLibrary)]
        static extern IntPtr CGWindowListCreateImage (CGRect screenBounds, CGWindowListOption windowOption,
            uint windowID, CGWindowImageOption imageOption);

        [InteractiveHelp (Description = "Return a screenshot of the given window")]
        public static CGImage Capture (NSWindow window)
        {
            if (window == null)
                throw new ArgumentNullException (nameof(window));

            var handle = CGWindowListCreateImage (
                CGRect.Empty, // captures the whole window
                CGWindowListOption.IncludingWindow,
                (uint)window.WindowNumber,
                CGWindowImageOption.BoundsIgnoreFraming // no shadows, etc.
            );

            return handle == IntPtr.Zero ? null : new CGImage (handle);
        }

        [InteractiveHelp (Description = "Return a screenshot of the given view")]
        public static CGImage Capture (NSView view)
        {
            if (view == null)
                throw new ArgumentNullException (nameof(view));

            var bitmap = view.BitmapImageRepForCachingDisplayInRect (view.Bounds);
            if (bitmap == null)
                return null;

            view.CacheDisplay (view.Bounds, bitmap);
            return bitmap.CGImage;
        }
    }
}