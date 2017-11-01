//
// FormsViewHierarchyHandler.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.


using CoreGraphics;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.iOS;

namespace Xamarin.Interactive.Forms.iOS
{
    class iOSFormsViewHierarchyHandler : IViewHierarchyHandler
    {
        const string TAG = nameof (iOSFormsViewHierarchyHandler);

        iOSAgent agent;
        UIView selectionLayer;

        public iOSFormsViewHierarchyHandler (iOSAgent agent)
        {
            this.agent = agent;
        }

        Rectangle GetNativeViewBounds (VisualElement visualElement)
        {
            var view = Platform.GetRenderer (visualElement).NativeView;
            var point = view.ConvertPointToView (new CGPoint (0, 0), null);
            return new Rectangle (point.X, point.Y, view.Frame.Width, view.Frame.Height);
        }

        void ResetHighlightOnView ()
        {
            if (selectionLayer != null) {
                selectionLayer.RemoveFromSuperview ();
                selectionLayer.Dispose ();
                selectionLayer = null;
            }
        }

        void DrawHighlightOnView (VisualElement visualElement)
        {
            var view = Platform.GetRenderer (visualElement).NativeView;
            selectionLayer = new UIView (new CGRect (CGPoint.Empty, view.Bounds.Size));
            selectionLayer.BackgroundColor = UIColor.Clear;
            selectionLayer.Layer.BorderColor = UIColor.Red.CGColor;
            selectionLayer.Layer.BorderWidth = 1;
            view.AddSubview (selectionLayer);
        }

        public bool TryGetRepresentedView (object view, bool withSubviews, out IInspectView representedView)
        {
            representedView = null;

            var uiView = view as UIView;
            if (uiView == null)
                return false;

            representedView = FormsInspectViewHelper.GetInspectView (
                uiView,
                v => v is UIWindow,
                (container, page) => new iOSFormsInspectView (container, page, withSubviews),
                page => new iOSFormsInspectView (page, withSubviews: withSubviews),
                v => {
                    // Return null if the passed view isn't a Forms view.
                    var visualElementRenderer = v as IVisualElementRenderer;
                    if (visualElementRenderer != null)
                        return new iOSFormsInspectView (visualElementRenderer.Element, withSubviews);
                    return null;
                },
                ex => new iOSFormsInspectView (ex),
                () => new iOSFormsRootInspectView { DisplayName = agent.Identity.ApplicationName });

            return representedView != null;
        }

        public bool TryGetHighlightedView (double x, double y, bool clear, out IInspectView highlightedView)
            => FormsInspectViewHelper.TryGetHighlightedView (
                x,
                y,
                clear,
                element => new iOSFormsInspectView (element, false),
                ResetHighlightOnView,
                DrawHighlightOnView,
                GetNativeViewBounds,
                out highlightedView);
    }
}
