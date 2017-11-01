//
// ViewRenderer.cs
//
// This class was adapted from the Xamarin iOS Designer:
//   Xamarin.Designer.iOS/MonoTouch.Design.Server/TypeSystem/Renderer.cs
//
// Authors:
//   Xamarin iOS Designer Team
//   Aaron Bockover <abock@xamarin.com>
//   Larry Ewing <lewing@xamarin.com>
//
// FIXME: account for iOS < v7.0 (c.f. TryRender)
//
// Copyright 2014 Xamarin Inc.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UIKit;
using CoreGraphics;
using CoreAnimation;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.iOS
{
	static class ViewRenderer
	{
		const string TAG = "ViewRenderer";

		public static ViewTransform GetViewTransform (CALayer layer)
		{
			if (layer == null)
				return null;

			var superLayer = layer.SuperLayer;
			if (layer.Transform.IsIdentity && (superLayer == null || superLayer.Transform.IsIdentity))
				return null;

			var superTransform = layer.SuperLayer?.GetChildTransform () ?? CATransform3D.Identity;

			return layer.GetLocalTransform ()
				.Concat (superTransform)
			        .ToViewTransform ();
		}

		public static byte [] AsPNGBytes (this UIImage image)
		{
			if (image == null)
				return null;

			var data = image.AsPNG ();
			if (data == null)
				return null;

			try {
				var result = new byte [data.Length];
				Marshal.Copy (data.Bytes, result, 0, (int)data.Length);
				return result;
			} finally {
				data.Dispose ();
			}
		}

		public static byte [] RenderAsPng (UIWindow window, object obj, nfloat scale, bool skipChildren = true)
		{
			using (var image = Render (window, obj, scale, skipChildren))
				return AsPNGBytes (image);
		}

		public static UIImage Render (UIWindow window, object obj, nfloat scale, bool skipChildren = true)
		{
			CGContext ctx = null;
			Exception error = null;

			var viewController = obj as UIViewController;
			if (viewController != null) {
				// NOTE: We rely on the window frame having been set to the correct size when this method is invoked.
				UIGraphics.BeginImageContextWithOptions (window.Bounds.Size, false, scale);
				ctx = UIGraphics.GetCurrentContext ();

				if (!TryRender (window, ctx, ref error)) {
					//FIXME: test/handle this case
					Log.Warning (TAG, $"TryRender failed on {window}");
				}

				// Render the status bar with the correct frame size
				UIApplication.SharedApplication.TryHideStatusClockView ();
				var statusbarWindow = UIApplication.SharedApplication.GetStatusBarWindow ();
				if (statusbarWindow != null/* && metrics.StatusBar != null*/) {
					statusbarWindow.Frame = window.Frame;
					statusbarWindow.Layer.RenderInContext (ctx);
				}
			}

			var view = obj as UIView;
			if (view != null) {
				UIGraphics.BeginImageContextWithOptions (view.Bounds.Size, false, scale);
				ctx = UIGraphics.GetCurrentContext ();
				// ctx will be null if the width/height of the view is zero
				if (ctx != null)
					TryRender (view, ctx, ref error);
			}

			var layer = obj as CALayer;
			if (layer != null) {
				UIGraphics.BeginImageContextWithOptions (layer.Bounds.Size, false, scale);
				ctx = UIGraphics.GetCurrentContext ();
				if (ctx != null)
					TryRender (layer, ctx, skipChildren, ref error);
			}

			if (ctx == null)
				return null;

			var image = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();
			return image;
		}

		static bool TryRender (UIView view, CGContext ctx, ref Exception error)
		{
			try {
				view.DrawViewHierarchy (view.Bounds, afterScreenUpdates: true);
				return true;
			} catch (Exception e) {
				error = e;
				return false;
			}
		}

		static Dictionary<IntPtr, bool> GetVisibilitySnapshotAndHideLayers (CALayer layer)
		{
			var visibilitySnapshot = new Dictionary<IntPtr, bool> ();
			layer.Sublayers?.ForEach (sublayer => {
				var subSnapshot = GetVisibilitySnapshotAndHideLayers (sublayer);
				foreach (var kvp in subSnapshot)
					visibilitySnapshot.Add (kvp.Key, kvp.Value);
				visibilitySnapshot.Add (sublayer.Handle, sublayer.Hidden);
				sublayer.Hidden = true;
			});
			return visibilitySnapshot;
		}

		static void ResetLayerVisibilitiesFromSnapshot (
			CALayer layer,
			Dictionary<IntPtr, bool> visibilitySnapshot)
		{
			layer.Sublayers?.ForEach (sublayer => {
				ResetLayerVisibilitiesFromSnapshot (sublayer, visibilitySnapshot);
				sublayer.Hidden = visibilitySnapshot [sublayer.Handle];
			});
		}

		static bool TryRender (CALayer layer, CGContext ctx, bool skipChildren, ref Exception error)
		{
			try {
				Dictionary<IntPtr, bool> visibilitySnapshot = null;
				if (skipChildren)
					visibilitySnapshot = GetVisibilitySnapshotAndHideLayers (layer);
				layer.RenderInContext (ctx);
				if (skipChildren)
					ResetLayerVisibilitiesFromSnapshot (layer, visibilitySnapshot);
				return true;
			} catch (Exception e) {
				error = e;
				return false;
			}
		}
	}
}