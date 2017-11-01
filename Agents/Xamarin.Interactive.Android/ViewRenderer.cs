//
// ViewRenderer.cs
//
// This class is based on iOS Agent ViewRenderer which was adapted from the Xamarin iOS Designer:
//   Xamarin.Designer.iOS/MonoTouch.Design.Server/TypeSystem/Renderer.cs
//
// Authors:
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2014 Xamarin Inc
// Copyright 2017 Microsoft.  All rights reserved.

using System;
using System.IO;

using Java.Interop;

using Android.Graphics;
using Android.Views;
using GL = Android.Opengl;
using Android.Runtime;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Android
{
	static class ViewRenderer
	{
		const string TAG = "ViewRenderer";

		static readonly Lazy<JniType> viewClass =
			new Lazy<JniType> (() => new JniType ("android/view/View"));

		static readonly Lazy<JniMethodInfo> viewCreateSnapshot =
			new Lazy<JniMethodInfo> (() => JniEnvironment.InstanceMethods.GetMethodID (
				viewClass.Value.PeerReference,
				"createSnapshot",
				"(Landroid/graphics/Bitmap$Config;IZ)Landroid/graphics/Bitmap;"));

		public static ViewTransform GetViewTransform (View view)
		{
			if (view == null || view.Matrix.IsIdentity)
				return null;
			
			var m = new float [16];
			var v = new float [16];
			var r = new float [16];

			GL.Matrix.SetIdentityM (r, 0);
			GL.Matrix.SetIdentityM (v, 0);
			GL.Matrix.SetIdentityM (m, 0);

			GL.Matrix.TranslateM (v, 0, view.Left, view.Top, 0);
			GL.Matrix.TranslateM (v, 0, view.PivotX, view.PivotY, 0);
			GL.Matrix.TranslateM (v, 0, view.TranslationX, view.TranslationY, 0);
			GL.Matrix.ScaleM (v, 0, view.ScaleX, view.ScaleY, 1);
			GL.Matrix.RotateM (v, 0, view.RotationX, 1, 0, 0);
			GL.Matrix.RotateM (v, 0, view.RotationY, 0, 1, 0);
			GL.Matrix.RotateM (m, 0, view.Rotation, 0, 0, 1);

			GL.Matrix.MultiplyMM (r, 0, v, 0, m, 0);
			GL.Matrix.TranslateM (m, 0, r, 0, -view.PivotX, -view.PivotY, 0);

			return new ViewTransform {
				M11 = m [0],
				M12 = m [1],
				M13 = m [2],
				M14 = m [3],
				M21 = m [4],
				M22 = m [5],
				M23 = m [6],
				M24 = m [7],
				M31 = m [8],
				M32 = m [9],
				M33 = m [10],
				M34 = m [11],
				OffsetX = m [12],
				OffsetY = m [13],
				OffsetZ = m [14],
				M44 = m [15]
			};
		}

		public static byte [] AsPNGBytes (this Bitmap bitmap)
		{
			byte[] byteArray = null;
			using (var mem = new MemoryStream ())
			{
				bitmap.Compress (Bitmap.CompressFormat.Png, 100, mem);
				byteArray = mem.ToArray ();
			}

			return byteArray;
		}

		public static byte [] RenderAsPng (View view, bool skipChildren)
		{
			byte[] pngBytes = null;

			using (var bitmap = Render (view, skipChildren)) {
				if (bitmap != null) {
					pngBytes = AsPNGBytes (bitmap);
					if (!bitmap.IsRecycled)
						bitmap.Recycle ();
				}
			}

			return pngBytes;
		}

		public static Bitmap Render (View view, bool skipChildren)
		{
			var bitmap = RenderUsingCreateSnapshot (view, skipChildren);

			// Canvas drawing and drawing cache can't skip children,
			// so don't pass the flag down to them.
			if (bitmap == null)
				bitmap = RenderUsingCanvasDrawing (view);

			if (bitmap == null)
				bitmap = RenderUsingDrawingCache (view);

			return bitmap;
		}

		static unsafe Bitmap RenderUsingCreateSnapshot (View view, bool skipChildren)
		{
			if (view.Width <= 0 || view.Height <= 0) {
				Log.Debug (TAG, $"Skipping createSnapshot rendering on view {view}, width or height are <= 0.");
				return null;
			}

			JniObjectReference o = default (JniObjectReference);
			try {
				// Invoke the createSnapshot package-private method.
				JniArgumentValue* args = stackalloc JniArgumentValue [3];
				args [0] = new JniArgumentValue (Bitmap.Config.Argb8888.PeerReference);
				args [1] = new JniArgumentValue (0);
				args [2] = new JniArgumentValue (skipChildren);
				o = JniEnvironment.InstanceMethods.CallObjectMethod (
					view.PeerReference,
					viewCreateSnapshot.Value,
					args);

				if (o.IsValid)
					return Java.Lang.Object.GetObject<Bitmap> (
						o.Handle,
						JniHandleOwnership.DoNotTransfer);

				return null;
			} catch (Exception e) {
				Log.Debug (TAG, $"CreateSnapshot failed: {e.Message}.");
				return null;
			} finally {
				JniObjectReference.Dispose (ref o);
			}
		}

		static Bitmap RenderUsingCanvasDrawing (View view)
		{
			try {
				var width = view.LayoutParameters.Width;
				var height = view.LayoutParameters.Height;
				view.Layout (0, 0, width, height);

				var bitmap = Bitmap.CreateBitmap (width, height, Bitmap.Config.Argb8888);

				using (var canvas = new Canvas (bitmap))
					view.Draw (canvas);

				return bitmap;
			} catch (Exception e) {
				Log.Debug (TAG, $"Failed to draw view on canvas: {e.Message}");
				return null;
			}
		}

		static Bitmap RenderUsingDrawingCache (View view)
		{
			try {
				var enabled = view.DrawingCacheEnabled;
				view.DrawingCacheEnabled = true;
				view.BuildDrawingCache ();
				var cachedBitmap = view.DrawingCache;
				var bitmap = Bitmap.CreateBitmap (cachedBitmap);
				view.DrawingCacheEnabled = enabled;
				return bitmap;
			} catch (Exception e) {
				Log.Debug (TAG, $"Failed to grab drawing cache: {e.Message}");
				return null;
			}
		}
	}
}
