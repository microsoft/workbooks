//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AndroidView = Android.Views.View;
using AndroidColor = Android.Graphics.Color;
using Android.Graphics.Drawables;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using Xamarin.Interactive.Android;
using Xamarin.Interactive.Inspection;

namespace Xamarin.Interactive.Forms.Android
{
    sealed class AndroidFormsViewHierarchyHandler : IViewHierarchyHandler
    {
        AndroidAgent agent;
        AndroidView highlightedView;
        Drawable highlightedViewOriginalBackground;

        public AndroidFormsViewHierarchyHandler (AndroidAgent agent)
        {
            this.agent = agent;
        }

        Rectangle GetNativeViewBounds (VisualElement visualElement)
        {
            var nativeView = Platform.GetRenderer (visualElement).ViewGroup;
            var location = new int [2];
            nativeView.GetLocationOnScreen (location);

            return new Rectangle (
                location [0],
                location [1],
                nativeView.Context.ToPixels (visualElement.Width),
                nativeView.Context.ToPixels (visualElement.Height));
        }

        void ResetHighlightOnView ()
        {
            if (highlightedView != null) {
#pragma warning disable CS0618 // Type or member is obsolete
                highlightedView.SetBackgroundDrawable (highlightedViewOriginalBackground);
#pragma warning restore CS0618 // Type or member is obsolete
                highlightedView = null;
                highlightedViewOriginalBackground = null;
            }
        }

        void DrawHighlightOnView (VisualElement element)
        {
            var view = Platform.GetRenderer (element).ViewGroup;

            highlightedView = view;
            highlightedViewOriginalBackground = highlightedView.Background;

            var gd = new GradientDrawable ();
            gd.SetColor (AndroidColor.Red.ToArgb ());
            gd.SetAlpha (255 / 2);

            Drawable highlightedBackground;
            if (highlightedViewOriginalBackground == null)
                highlightedBackground = gd;
            else
                highlightedBackground =
                    new LayerDrawable (new [] { highlightedViewOriginalBackground, gd });

#pragma warning disable CS0618 // Type or member is obsolete
            highlightedView.SetBackgroundDrawable (highlightedBackground);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public bool TryGetHighlightedView (double x, double y, bool clear, out IInspectView highlightedView)
            => FormsInspectViewHelper.TryGetHighlightedView (
                x,
                y,
                clear,
                element => new AndroidFormsInspectView (element, false),
                ResetHighlightOnView,
                DrawHighlightOnView,
                GetNativeViewBounds,
                out highlightedView);

        public bool TryGetRepresentedView (object view, bool withSubviews, out IInspectView representedView)
        {
            representedView = null;

            var androidView = view as AndroidView;
            if (androidView == null)
                return false;

            representedView = FormsInspectViewHelper.GetInspectView (
                androidView,
                v => v == agent.GetTopActivity ()?.Window?.DecorView,
                (container, page) => new AndroidFormsInspectView (container, page, withSubviews),
                page => new AndroidFormsInspectView (page, withSubviews: withSubviews),
                v => {
                    // Return null if the passed view isn't a Forms view.
                    var visualElementRenderer = v as IVisualElementRenderer;
                    if (visualElementRenderer != null)
                        return new AndroidFormsInspectView (visualElementRenderer.Element, withSubviews);
                    return null;
                },
                ex => new AndroidFormsInspectView (ex),
                () => new AndroidFormsRootInspectView { DisplayName = agent.Identity.ApplicationName });

            return representedView != null;
        }
    }
}