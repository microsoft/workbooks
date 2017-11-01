//
// VisualRepViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using SceneKit;

using Xamarin.Interactive.Client.Mac.ViewInspector;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class VisualRepViewController : ViewInspectorViewController
    {
        const string TAG = nameof (VisualRepViewController);

        ViewDepth viewDepth = ViewDepth.ThreeDimensional;
        public ViewDepth ViewDepth {
            get { return viewDepth; }
            set {
                if (value != viewDepth)
                    SwitchDepth (value);
                viewDepth = value;
            }
        }

        VisualRepViewController (IntPtr handle) : base (handle)
        {
        }

        void SwitchDepth (ViewDepth depth)
        {
            if (depth == ViewDepth.TwoDimensional) {
                scnView.Trackball.PushSettings ();
                scnView.Trackball.Around = new System.Numerics.Quaternion (0, 0, 1, 0);
                scnView.Trackball.Reset (() => {
                    scnView.PointOfView.Camera.UsesOrthographicProjection = true;
                    scnView.PointOfView.Camera.OrthographicScale = .6;
                    scnView.PointOfView.Camera.ZNear = 0.01;
                    scnView.PointOfView.Camera.ZFar = 1000;
                });
            } else {
                scnView.PointOfView.Camera.UsesOrthographicProjection = false;
                scnView.Trackball.PopSettings ();
            }
        }

        public void ResetCamera ()
        {
            scnView.Trackball.Reset ();
        }

        public override void ViewDidLoad ()
        {
            scnView.ViewSelected += ParentViewController.SelectView;
            base.ViewDidLoad ();
        }

        protected override void OnRepresentedViewChanged ()
        {
            scnView.CurrentView = RepresentedView;
        }
    }
}