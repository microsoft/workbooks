//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UIKit;
using ObjCRuntime;
using CoreGraphics;
using CoreAnimation;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.iOS
{
    static class NativeExtensions
    {
        [DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        static extern bool bool_objc_msgSend_IntPtr (IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        static extern IntPtr IntPtr_objc_msgSend (IntPtr receiver, IntPtr selector);

        static class Selectors
        {
            public static readonly Selector statusBarWindow = new Selector ("statusBarWindow");
            public static readonly Selector respondsToSelector = new Selector ("respondsToSelector:");
        }

        public static UIWindow GetStatusBarWindow (this UIApplication app)
        {
            if (!bool_objc_msgSend_IntPtr (app.Handle,
                Selectors.respondsToSelector.Handle,
                Selectors.statusBarWindow.Handle))
                return null;

            var ptr = IntPtr_objc_msgSend (app.Handle, Selectors.statusBarWindow.Handle);
            return ptr != IntPtr.Zero ? (UIWindow)ObjCRuntime.Runtime.GetNSObject (ptr) : null;
        }

        public static void TryHideStatusClockView (this UIApplication app)
        {
            var statusBarWindow = app.GetStatusBarWindow ();
            if (statusBarWindow == null)
                return;

            var clockView = statusBarWindow.FindSubview (
                "UIStatusBar",
                "UIStatusBarForegroundView",
                "UIStatusBarTimeItemView"
            );

            if (clockView != null)
                clockView.Hidden = true;
        }

        public static UIView FindSubview (this UIView view, params string[] classNames)
        {
            return FindSubview (view, ((IEnumerable<string>)classNames).GetEnumerator ());
        }

        static UIView FindSubview (UIView view, IEnumerator<string> classNames)
        {
            if (!classNames.MoveNext ())
                return view;

            foreach (var subview in view.Subviews) {
                if (subview.ToString ().StartsWith ("<" + classNames.Current + ":", StringComparison.Ordinal))
                    return FindSubview (subview, classNames);
            }

            return null;
        }

        public static UIView FindLayerView (this UIView root, CALayer layer)
        {
            var memo = new Dictionary<IntPtr, UIView> ();
            foreach (var view in root.TraverseTree (v => v.Subviews)) {
                if (view.Layer == layer)
                    return view;

                if (view.Layer != null)
                    memo [view.Layer.Handle] = view;
            }

            var superLayer = layer.SuperLayer;
            while (superLayer != null) {
                UIView viewParent;

                if (memo.TryGetValue (superLayer.Handle, out viewParent))
                    return viewParent;

                superLayer = superLayer.SuperLayer;
            }
            return null;
        }

        public static Image RemoteRepresentation (this UIView uiview)
        {
            var scale = UIScreen.MainScreen.Scale;
            return ViewRenderer.Render (
                uiview.Window,
                uiview,
                scale).RemoteRepresentation (scale);
        }
    }

    public static class GraphicsExtensions
    {
        public static Image RemoteRepresentation (this UIImage image)
            => RemoteRepresentation (image, 1f);

        public static Image RemoteRepresentation (this UIImage image, nfloat scale)
            => image == null ? null : new Image (
                ImageFormat.Png,
                image.AsPNGBytes (),
                (int)image.CGImage.Width,
                (int)image.CGImage.Height,
                scale);

        public static Image RemoteRepresentation (this CGImage image)
            => image == null ? null : new UIImage (image).RemoteRepresentation ();
    }
}