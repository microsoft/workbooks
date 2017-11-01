//
// MacInspectView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using AppKit;
using CoreAnimation;
using CoreGraphics;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.Mac
{
    [Serializable]
    class MacRootInspectView : InspectView
    {
        public MacRootInspectView ()
        {
            SetHandle (IntPtr.Zero);

            foreach (var window in NSApplication.SharedApplication.Windows)
                if (window.IsVisible)
                    AddSubview (new MacInspectView (window.ContentView) {
                        DisplayName = window.Title
                    });
        }

        protected MacRootInspectView (SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }

        protected override void UpdateCapturedImage ()
        {
            // TODO
        }
    }

    [Serializable]
    class MacInspectView : InspectView
    {
        readonly NSView view;
        readonly CALayer layer;

        protected MacInspectView (SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }

        public new MacInspectView Parent {
            get { return (MacInspectView)base.Parent; }
            set { base.Parent = value; }
        }

        public new MacInspectView Root {
            get { return (MacInspectView)base.Root; }
        }

        /// <summary>
		/// Creates a new <see cref="T:Xamarin.Interactive.Mac.MacInspectView"/> that represents the given layer.
		/// </summary>
		/// <param name="layerParent">The parent view of the layer.</param>
		/// <param name="layer">The layer itself.</param>
		/// <param name="visitedLayers">
		/// Layers we've already visited in building this tree. This method both checks sublayers against this
		/// collection and modifies it by adding sublayers it consumes.
		/// </param>
		/// <remarks>
		/// We need to keep track of this set of visited layers to avoid an oddity in the way that layers are
		/// presented in the AppKit "tree." Layers and views are actually two separate trees, with the layer
		/// tree being a child tree of the top-level window, and the individual layer properties of each
		/// UIView being pointers into various places in that tree.
		/// </remarks>
        public MacInspectView (NSView layerParent, CALayer layer, HashSet<IntPtr> visitedLayers)
        {
            if (layerParent == null)
                throw new ArgumentNullException (nameof (layerParent));
            if (layer == null)
                throw new ArgumentNullException (nameof (layer));

            this.layer = layer;
            this.view = layerParent;

            SetHandle (ObjectCache.Shared.GetHandle (layer));
            PopulateTypeInformationFromObject (layer);

            Description = layer.Description;

            var superTransform = layer.SuperLayer?.GetChildTransform () ?? CATransform3D.Identity;

            // on iOS the layer transform has the view transform already applied
            // on macOs we have to composite it ourself if we are skipping the view
            // node in the inspector tree
            if (layerParent.Layer == layer && layer.SuperLayer == null) {
                if (layerParent.IsFlipped)
                    superTransform = GetLocalTransform (layerParent);
                else
                    superTransform = GetLocalTransform (layerParent)
                        .Translate (0, layer.Bounds.Height, 0)
                        .Scale (1, -1, 1);
            }

            if (!layer.Transform.IsIdentity || !superTransform.IsIdentity) {
                Transform = layer
                    .GetLocalTransform ()
                    .Concat (superTransform)
                    .ToViewTransform ();

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
            // macOS doesn't have a concept of hidden but laid out, so it's either collapsed or visible.
            Visibility = layer.Hidden ? ViewVisibility.Collapsed : ViewVisibility.Visible;

            var sublayers = layer.Sublayers;
            if (sublayers?.Length > 0) {
                for (int i = 0; i < sublayers.Length; i++) {
                    var subLayer = sublayers [i];
                    if (!visitedLayers.Contains (subLayer.Handle)) {
                        base.AddSublayer (new MacInspectView (layerParent, subLayer, visitedLayers));
                        visitedLayers.Add (subLayer.Handle);
                    }
                }
            }
        }

        public MacInspectView (NSView view)
        {
            if (view == null)
                throw new ArgumentNullException (nameof (view));

            this.view = view;

            PopulateTypeInformationFromObject (view);

            // FIXME: special case certain view types and fill in the Description property
            var flipped = view.Superview.IsFlipped;
            if (view.FrameRotation != 0) {
                Transform = GetLocalTransform (view)
                    .ToViewTransform ();
                X = view.Bounds.X;
                Y = view.Bounds.Y;
                Width = view.Bounds.Width;
                Height = view.Bounds.Height;
            } else {
                X = view.Frame.X;
                Y = flipped ? view.Frame.Y : view.Superview.Frame.Height - (view.Frame.Y + view.Frame.Height);
                Width = view.Frame.Width;
                Height = view.Frame.Height;
            }
            Kind = ViewKind.Primary;
            // macOS doesn't have a concept of hidden but laid out, so it's either collapsed or visible.
            Visibility = view.Hidden ? ViewVisibility.Collapsed : ViewVisibility.Visible;

            var visitedLayers = new HashSet<IntPtr> ();

            var subviews = view.Subviews;
            if (subviews != null && subviews.Length > 0) {
                for (int i = 0; i < subviews.Length; i++) {
                    var subview = new MacInspectView (subviews [i]);
                    base.AddSubview (subview);

                    if (subview.Layer == null)
                        continue;

                    // After calling AddSubview, add any visited layers to the list. We track
                    // visited layers here so that when we actually recurse into the layer that
                    // belongs to this view, we don't duplicate things. This is needed because of
                    // the pointer-into-a-tree nature of layers, as explained above in the constructor
                    // remarks.
                    var subviewLayer = (MacInspectView)subview.Layer;
                    if (subviewLayer.layer != null)
                        visitedLayers.Add (subviewLayer.layer.Handle);

                    subviewLayer.layer?.Sublayers?.ForEach (
                        layer => visitedLayers.Add (layer.Handle));
                }
            }

            if (view.WantsLayer && !visitedLayers.Contains (view.Layer.Handle))
                Layer = new MacInspectView (view, view.Layer, visitedLayers) { Parent = this };
        }

        public NSView UpdateSelection (NSView selectionLayerView)
        {
            if (selectionLayerView.Superview != view) {
                if (selectionLayerView.Superview != null)
                    selectionLayerView.RemoveFromSuperview ();

                view.AddSubview (selectionLayerView);
            }

            var bounds = new CGRect (CGPoint.Empty, view.Bounds.Size);
            selectionLayerView.Frame = bounds;
            var transform = CATransform3D.Identity;
            var selectionLayer = selectionLayerView.Layer.Sublayers [0];

            var targetLayer = layer;
            if (targetLayer != null) {
                var hostLayer = view.Layer;
                bounds = targetLayer.Bounds;
                transform = targetLayer.TransformToAncestor (hostLayer);
                if (!hostLayer.SublayerTransform.IsIdentity)
                    transform = transform.Concat (
                            hostLayer.GetChildTransform ().Invert ());
                
            }

            selectionLayer.Transform = transform;
            selectionLayer.Bounds = bounds;
            selectionLayer.AnchorPoint = CGPoint.Empty;
            selectionLayer.Position = CGPoint.Empty;
            return selectionLayerView;
        }

        static CATransform3D GetLocalTransform (NSView view)
        {
            var flipped = view.Superview.IsFlipped;
            if (view.FrameRotation == 0)
                return CATransform3D.MakeTranslation (
                    view.Frame.X,
                    flipped ? view.Frame.Y : view.Superview.Bounds.Height - (view.Frame.Y + view.Frame.Height),
                    0);

            return CATransform3D.Identity
                .Scale (1, -1, 1)
                .Rotate ((float)Math.PI * view.FrameRotation / 180f, 0, 0, 1)
                .Translate (
                    view.Bounds.X,
                        flipped ? -view.Bounds.Y : view.Bounds.Y + view.Bounds.Height,
                    0)
                .Scale (1, -1, 1)
                .Translate (0, flipped ? 0 : view.Superview.Bounds.Height, 0);
        }

        protected override void UpdateCapturedImage ()
        {
            if (view != null && layer == null) {
                var bitmap = view.BitmapImageRepForCachingDisplayInRect (view.Bounds);
                if (bitmap == null)
                    return;

                view.CacheDisplay (view.Bounds, bitmap);
                var data = bitmap.RepresentationUsingTypeProperties (NSBitmapImageFileType.Png);
                CapturedImage = data.ToArray ();
            } else if (layer != null) {
                var scale = layer.ContentsScale;
                nint h = (nint)(layer.Bounds.Height * scale);
                nint w = (nint)(layer.Bounds.Width * scale);
                nint bytesPerRow = w * 4;

                if (h <= 0 || w <= 0)
                    return;

                using (var colorSpace = CGColorSpace.CreateGenericRgb ())
                using (var context = new CGBitmapContext (IntPtr.Zero, w, h, 8, bytesPerRow, colorSpace, CGImageAlphaInfo.PremultipliedLast)) {
                    // Apply a flipping transform because layers are apparently weird.
                    var transform = new CGAffineTransform (scale, 0, 0, -scale, 0, h);
                    context.ConcatCTM (transform);

                    layer.RenderInContext (context);

                    using (var image = context.ToImage ())
                    using (var bitmap = new NSBitmapImageRep (image)) {
                        var data = bitmap.RepresentationUsingTypeProperties (NSBitmapImageFileType.Png);
                        CapturedImage = data.ToArray ();
                    }
                }
            }
        }
    }
}
