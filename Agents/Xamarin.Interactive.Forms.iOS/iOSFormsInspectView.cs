//
// FormsInspectView.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.


using System;
using System.Runtime.Serialization;

using UIKit;
using CoreAnimation;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Remote;

using XIVR = Xamarin.Interactive.iOS.ViewRenderer;
using static Xamarin.Interactive.Forms.FormsInspectViewHelper;

namespace Xamarin.Interactive.Forms.iOS
{
    [Serializable]
    class iOSFormsRootInspectView : InspectView
    {
        public iOSFormsRootInspectView ()
        {
            SetHandle (IntPtr.Zero);
        }

        protected iOSFormsRootInspectView (SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }

        protected override void UpdateCapturedImage ()
        {
            // TODO
        }
    }

    [Serializable]
    class iOSFormsInspectView : InspectView
    {
        const string TAG = nameof (iOSFormsInspectView);
        readonly Element element;

        static TypeMap<Func<Element, string>> supplementaryDescriptionMap = new TypeMap<Func<Element, string>> ();

        static iOSFormsInspectView ()
        {
            supplementaryDescriptionMap.Add (typeof (NativeViewWrapper), true, (arg) => {
                return ((NativeViewWrapper)arg).NativeView.GetType ().Name;
            });
        }

        protected iOSFormsInspectView (SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }

        public new iOSFormsInspectView Parent {
            get { return (iOSFormsInspectView)base.Parent; }
            set { base.Parent = value; }
        }

        public new iOSFormsInspectView Root {
            get { return (iOSFormsInspectView)base.Root; }
        }

        public iOSFormsInspectView (Exception ex)
        {
            DisplayName = ex == null
                ? "No Xamarin.Forms hierarchy available"
                : "Error while getting hierarchy";
        }

        public iOSFormsInspectView (Page container, Page page, bool withSubviews = true)
        {
            if (container == null)
                throw new ArgumentNullException (nameof (container));

            if (page == null)
                throw new ArgumentNullException (nameof (page));

            element = container;

            PopulateTypeInformationFromObject (container);
            var view = Platform.GetRenderer (container).NativeView;
            var layer = view.Layer;

            Transform = XIVR.GetViewTransform (layer);
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

            Kind = ViewKind.Primary;
            Visibility = container.GetViewVisibility ();

            HandleContainerChildren (
                container,
                page,
                p => new iOSFormsInspectView (p, true, withSubviews),
                e => new iOSFormsInspectView (e, withSubviews),
                AddSubview
            );
        }

        public iOSFormsInspectView (Page page, bool useNativeViewBounds = false, bool withSubviews = true)
        {
            if (page == null)
                throw new ArgumentNullException (nameof (page));

            element = page;

            PopulateTypeInformationFromObject (page);

            // TODO: Pull the ClassId or some user-set property as the description?
            var view = Platform.GetRenderer (page).NativeView;

            if (!useNativeViewBounds) {
                var layer = view.Layer;

                Transform = XIVR.GetViewTransform (layer);
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
            } else {
                var convertedOffset = view.ConvertPointToView (view.Frame.Location, view.Window);
                X = convertedOffset.X;
                Y = convertedOffset.Y;
            }

            Width = page.Bounds.Width;
            Height = page.Bounds.Height;
            Visibility = page.GetViewVisibility ();
            Kind = ViewKind.Primary;

            if (withSubviews)
                HandlePageChildren (
                    page,
                    (p, b) => new iOSFormsInspectView (p, useNativeViewBounds || b, withSubviews),
                    e => new iOSFormsInspectView (e, withSubviews),
                    AddSubview
                );
        }

        public iOSFormsInspectView (Element element, bool withSubviews = true)
        {
            if (element == null)
                throw new ArgumentNullException (nameof (element));

            this.element = element;

            PopulateTypeInformationFromObject (element);

            var velement = element as VisualElement;
            if (velement != null) {
                Visibility = velement.GetViewVisibility ();

                var view = Platform.GetRenderer (velement).NativeView;
                var layer = view.Layer;

                Transform = XIVR.GetViewTransform (layer);
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
            } else {
                // Since this is not a visual element, set it as collapsed by default.
                Visibility = ViewVisibility.Collapsed;
            }

            Kind = ViewKind.Primary;

            // TODO: Figure out different types of elements and extra useful data from them when appropriate.
            Description = GetDescriptionFromElement (element, supplementaryDescriptionMap);

            if (withSubviews)
                HandleElementChildren (element, arg => new iOSFormsInspectView (arg, withSubviews), AddSubview);
        }

        protected override void UpdateCapturedImage ()
        {
            VisualElement ve;
            if ((ve = element as VisualElement) != null) {
                var nativeView = Platform.GetRenderer (ve).NativeView;
                var skipChildren = !(ve is View && !(ve is Layout));
                if (nativeView != null)
                    CapturedImage = XIVR.RenderAsPng (
                        nativeView.Window,
                        nativeView.Layer,
                        UIScreen.MainScreen.Scale,
                        skipChildren);
            }
        }
    }
}
