//
// iOSInspectView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.iOS
{
	[Serializable]
	class iOSInspectView : InspectView
	{
		readonly UIView view;
		readonly CALayer layer;

		public NSObject Material { get; set; }

		public new iOSInspectView Parent {
			get { return (iOSInspectView)base.Parent; }
			set { base.Parent = value; }
		}

		public new iOSInspectView Root {
			get { return (iOSInspectView)base.Root; }
		}

		public iOSInspectView ()
		{
		}

		/// <summary>
		/// Creates a new <see cref="T:Xamarin.Interactive.iOS.iOSInspectView"/> that represents the given layer.
		/// </summary>
		/// <param name="parent">The parent view of the layer.</param>
		/// <param name="layer">The layer itself.</param>
		/// <param name="visitedLayers">
		/// Layers we've already visited in building this tree. This method both checks sublayers against this
		/// collection and modifies it by adding sublayers it consumes.
		/// </param>
		/// <param name="withSublayers">If <c>true</c>, descend into sublayers of this layer.</param>
		/// <remarks>
		/// We need to keep track of this set of visited layers to avoid an oddity in the way that layers are
		/// presented in the UIKit "tree." Layers and views are actually two separate trees, with the layer
		/// tree being a child tree of the top-level UIWindow, and the individual layer properties of each
		/// UIView being pointers into various places in that tree.
		/// </remarks>
		public iOSInspectView (UIView parent, CALayer layer, HashSet<IntPtr> visitedLayers, bool withSublayers = true)
		{
			if (parent == null)
				throw new ArgumentNullException (nameof (parent));
			if (layer == null)
				throw new ArgumentNullException (nameof (layer));

			view = parent;
			this.layer = layer;

			SetHandle (ObjectCache.Shared.GetHandle (layer));
			PopulateTypeInformationFromObject (layer);

			Description = layer.Description;

			Transform = ViewRenderer.GetViewTransform (layer);
			if (Transform != null) {
				X = layer.Bounds.X;
				Y = layer.Bounds.Y;
				Width = layer.Bounds.Width;
				Height = layer.Bounds.Height;
			} else {
				X = layer.Frame.X;
				Y = layer.Frame.Y;
				Width = layer.Frame.Width;
				Height = layer.Frame.Height;
			}

			Kind = ViewKind.Secondary;
			// iOS doesn't have a concept of hidden but laid out, so it's either collapsed or visible.
			Visibility = layer.Hidden ? ViewVisibility.Collapsed : ViewVisibility.Visible;

			if (!withSublayers) {
				var point = view.ConvertPointToView (
					new CoreGraphics.CGPoint (X, Y),
					null);

				X = point.X;
				Y = point.Y;
				return;
			}

			var sublayers = layer.Sublayers;
			if (sublayers != null && sublayers.Length > 0) {
				for (int i = 0; i < sublayers.Length; i++)
					if (!visitedLayers.Contains (sublayers [i].Handle)) {
						AddSublayer (new iOSInspectView (parent, sublayers [i], visitedLayers));
						visitedLayers.Add (sublayers [i].Handle);
					}
			}
		}

		public iOSInspectView (UIView view, bool withSubviews = true)
		{
			if (view == null)
				throw new ArgumentNullException (nameof (view));

			this.view = view;

			PopulateTypeInformationFromObject (view);

			// FIXME: special case certain view types and fill in the Description property

			if (view is UILabel)
				Description = ((UILabel)view).Text;
			else if (view is UIButton)
				Description = ((UIButton)view).TitleLabel.Text;
			else if (view is UITextField)
				Description = ((UITextField)view).Text;

			if (!view.Transform.IsIdentity) {
				var transform = CGAffineTransform.MakeIdentity ();
				transform.Translate (-view.Bounds.Width * .5f, -view.Bounds.Height * .5f);
				transform = CGAffineTransform.Multiply (transform, view.Transform);
				transform.Translate (view.Center.X , view.Center.Y);
				Transform = new ViewTransform {
					M11 = transform.xx,
					M12 = transform.yx,
					M21 = transform.xy,
					M22 = transform.yy,
					OffsetX = transform.x0,
					OffsetY = transform.y0
				};
				X = view.Bounds.X;
				Y = view.Bounds.Y;
				Width = view.Bounds.Width;
				Height = view.Bounds.Height;
			} else {
				X = view.Frame.X;
				Y = view.Frame.Y;
				Width = view.Frame.Width;
				Height = view.Frame.Height;
			}
			Kind = ViewKind.Primary;
			Visibility = view.Hidden ? ViewVisibility.Collapsed : ViewVisibility.Visible;

			if (!withSubviews) {
				var point = view.ConvertPointToView (
					new CoreGraphics.CGPoint (0, 0),
					null);

				X = point.X;
				Y = point.Y;
				return;
			}

			// MKMapView has a subview that is so large (5901507x5901507 in the case encountered)
			// that it causes the SceneKit camera to zoom out so much that every other node is
			// effectively hidden. This should ideally be fixed in the client.
			if (view is MapKit.MKMapView)
				return;

			var visitedLayers = new HashSet<IntPtr> ();

			var subviews = view.Subviews;
			if (subviews != null && subviews.Length > 0) {
				for (int i = 0; i < subviews.Length; i++) {
					var subview = new iOSInspectView (subviews [i]);
					AddSubview (subview);

					if (subview.Layer == null)
						continue;

					// After calling AddSubview, add any visited layers to the list. We track
					// visited layers here so that when we actually recurse into the layer that
					// belongs to this view, we don't duplicate things. This is needed because of
					// the pointer-into-a-tree nature of layers, as explained above in the constructor
					// remarks.
					var subviewLayer = (iOSInspectView)subview.Layer;
					if (subviewLayer.layer != null)
						visitedLayers.Add (subviewLayer.layer.Handle);

					subviewLayer.layer?.Sublayers?.ForEach (
						layer => visitedLayers.Add (layer.Handle));
				}
			}

			if (view.Layer != null && !visitedLayers.Contains (view.Layer.Handle))
				Layer = new iOSInspectView (view, view.Layer, visitedLayers) { Parent = this };
		}

		public CALayer UpdateSelection (CALayer selectionLayer, CALayer selectionHostLayer = null)
		{
			var targetLayer = layer ?? view.Layer;
			var hostLayer = selectionHostLayer ?? targetLayer;

			if (selectionLayer.SuperLayer != hostLayer) {
				if (selectionLayer.SuperLayer != null)
					selectionLayer.RemoveFromSuperLayer ();

				hostLayer.AddSublayer (selectionLayer);
			}

			selectionLayer.Bounds = targetLayer.Bounds;
			selectionLayer.AnchorPoint = CGPoint.Empty;
			selectionLayer.Transform = targetLayer.TransformToAncestor (hostLayer);
			selectionLayer.Position = CGPoint.Empty;

			if (!hostLayer.SublayerTransform.IsIdentity)
				selectionLayer.Transform = selectionLayer.Transform.Concat (
				    hostLayer.GetChildTransform ().Invert ());

			return selectionLayer;
		}

		public UIImage Capture (float? scale = null)
		{
			return ViewRenderer.Render (view.Window, view, scale == null ? UIScreen.MainScreen.Scale : scale.Value);
		}

		protected override void UpdateCapturedImage ()
		{
			// This is somewhat misnomered because it's not _just_ a ViewRenderer anymore, but
			// changing it seems unnecesssary.
			if (view != null && layer == null)
				CapturedImage = ViewRenderer.RenderAsPng (view.Window, view, UIScreen.MainScreen.Scale);
			else if (layer != null)
				CapturedImage = ViewRenderer.RenderAsPng (view.Window, layer, UIScreen.MainScreen.Scale);
		}
	}
}