//
// Authors:
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Android.Views;
using Android.Widget;
using Android.Opengl;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Android
{
    [Serializable]
    class AndroidInspectView : InspectView
    {
        const string androidIdPrefix = "android:";

        View view;

        public new AndroidInspectView Parent {
            get { return (AndroidInspectView)base.Parent; }
        }

        public new AndroidInspectView Root {
            get { return (AndroidInspectView)base.Root; }
        }

        public AndroidInspectView ()
        {
        }

        public AndroidInspectView (View view, bool withSubviews = true)
        {
            if (view == null)
                throw new ArgumentNullException (nameof (view));

            this.view = view;

            // FIXME: special case certain view types and fill in the Description property
            if (withSubviews) {
                Transform = ViewRenderer.GetViewTransform (view);
                if (Transform == null) {
                    X = view.Left;
                    Y = view.Top;
                }
            } else {
                var locArray = new int [2];
                view.GetLocationOnScreen (locArray);
                X = locArray [0];
                Y = locArray [1];
            }

            Width = view.Width;
            Height = view.Height;
            Kind = ViewKind.Primary;
            Visibility = view.Visibility.ToViewVisibility ();

            PopulateTypeInformationFromObject (view);

            DisplayName = view.GetType ().Name;
            try
            {
                DisplayName += " :" + view.Resources.GetResourceName(view.Id).TrimId();
            }
            catch
            {    }

            if (view is Button)
            {
                Description = ((Button)view).Text;
            }
            else if (view is TextView)
            {
                Description = ((TextView)view).Text;
            }

            if (!withSubviews)
                return;

            var subviews = view.Subviews();
            if (subviews != null && subviews.Length > 0) {
                for (int i = 0; i < subviews.Length; i++)
                    AddSubview (new AndroidInspectView (subviews [i]));
            }
        }

        public Image Capture (float? scale = null)
        {
            return null;
        }

        protected override void UpdateCapturedImage ()
        {
            CapturedImage = ViewRenderer.RenderAsPng (view, true);
        }
    }
}
