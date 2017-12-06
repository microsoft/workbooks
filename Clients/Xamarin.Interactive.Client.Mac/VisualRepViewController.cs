//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using SceneKit;

using Xamarin.Interactive.Client.Mac.ViewInspector;
using Xamarin.Interactive.Client.ViewInspector;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class VisualRepViewController : ViewInspectorViewController
    {
        const string TAG = nameof (VisualRepViewController);

        RenderingDepth renderingDepth = RenderingDepth.ThreeDimensional;
        public RenderingDepth Depth {
            get { return renderingDepth; }
            set {
                if (value != renderingDepth)
                    SwitchDepth (value);
                renderingDepth = value;
            }
        }

        VisualRepViewController (IntPtr handle) : base (handle)
        {
        }

        void SwitchDepth (RenderingDepth depth)
        {
            if (depth == RenderingDepth.TwoDimensional) {
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
            scnView.ViewSelected += (node) => Tree.SelectedNode = node;
            base.ViewDidLoad ();
        }

        protected override void OnRepresentedNodeChanged ()
        {
            var represented = Tree?.RepresentedNode;
            if (represented != null)
                if (represented.View.IsFakeRoot)
                    represented = represented.Children.OfType<InspectTreeNode> ().FirstOrDefault ();
            
            scnView.RepresentedNode = represented;
        }
    }
}