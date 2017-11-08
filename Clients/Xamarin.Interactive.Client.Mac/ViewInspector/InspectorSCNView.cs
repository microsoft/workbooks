//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

using Foundation;
using AppKit;
using SceneKit;

using Xamarin.Interactive.Remote;
using System.Numerics;

namespace Xamarin.Interactive.Client.Mac.ViewInspector
{
    [Register ("InspectSCNView")]
    sealed class InspectorSCNView : SCNView
    {
        const string TAG = nameof (InspectorSCNView);

        InspectViewNode currentViewNode;
        IInspectViewNode focusedNode;
        bool isDragging;
        DisplayMode displayMode = DisplayMode.FramesAndContent;
        bool showHiddenViews;

        public SceneKitDolly Trackball { get; } = new SceneKitDolly ();

        public event Action<InspectView> ViewSelected;

        public InspectorSCNView (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public InspectorSCNView (NSCoder coder) : base (coder)
        {
        }

        public bool ShowHiddenViews {
            get { return showHiddenViews; }
            set {
                showHiddenViews = value;
                RebuildScene (CurrentView, false);
            }
        }

        public DisplayMode DisplayMode {
            get { return displayMode; }
            set {
                displayMode = value;
                RebuildScene (CurrentView, false);
            }
        }

        public InspectView CurrentView {
            get { return currentViewNode?.InspectView; }
            set { RebuildScene (value); }
        }

        void RebuildScene (InspectView view, bool recreateScene = true)
        {
            if (recreateScene)
                Scene = new SCNScene ();
            else
                currentViewNode?.RemoveFromParentNode ();

            currentViewNode = null;

            if (view != null) {
                currentViewNode = new InspectViewNode (view).Rebuild (
                    new TreeState (DisplayMode, ShowHiddenViews));

                Scene.Add (currentViewNode);

                Trackball.Target = Scene.RootNode;
            }

            Play (this);
        }

        public override void MouseMoved (NSEvent theEvent)
        {
            var inspectViewNode = HitTest (
                ConvertPointFromView (theEvent.LocationInWindow, null),
                (SCNHitTestOptions)null
            )?.FirstOrDefault ()?.Node as IInspectViewNode;

            if (focusedNode != inspectViewNode) {
                focusedNode?.Blur ();
                focusedNode = inspectViewNode;
                focusedNode?.Focus ();
            }

            base.MouseMoved (theEvent);
        }

        public override void MouseDown (NSEvent theEvent)
        {
            base.MouseDown (theEvent);
            isDragging = false;
            Trackball.StartDrag (ConvertPointFromView (theEvent.LocationInWindow, null), Frame.Size);
        }

        public override void MouseDragged (NSEvent theEvent)
        {
            if (theEvent.ModifierFlags.HasFlag (NSEventModifierMask.CommandKeyMask))
                Trackball.DragZoom (ConvertPointFromView (theEvent.LocationInWindow, null), (float)Frame.Width, (float)Frame.Height);
            else
                Trackball.DragRotate (ConvertPointFromView (theEvent.LocationInWindow, null), (float)Frame.Width, (float)Frame.Height);
            
            base.MouseDragged (theEvent);
            isDragging = true;
        }

        public override void MouseUp (NSEvent theEvent)
        {
            base.MouseUp (theEvent);

            if (isDragging)
                return;

            var inspectView = focusedNode?.InspectView;
            if (inspectView != null)
                ViewSelected?.Invoke (inspectView);
        }

        public override void MagnifyWithEvent (NSEvent theEvent)
        {
            base.MagnifyWithEvent (theEvent);
            var magnification = theEvent.Magnification;
            var zoom = new SCNVector3 (magnification, magnification, magnification);
            Trackball.Zoom (zoom);
        }

        public override void ScrollWheel (NSEvent theEvent)
        {
            base.ScrollWheel (theEvent);

            if (theEvent.ModifierFlags.HasFlag (NSEventModifierMask.CommandKeyMask)) {
                float zoom = -(float)(theEvent.ScrollingDeltaY / Frame.Height);
                Trackball.Zoom (new SCNVector3 (zoom, zoom, zoom)); 
            } else 
                Trackball.Pan (new Vector3 (
                    (float)(theEvent.ScrollingDeltaX / Frame.Width),
                    (float)(-theEvent.ScrollingDeltaY / Frame.Height),
                    0));
        }

        public override void RotateWithEvent (NSEvent theEvent)
        {
            base.RotateWithEvent (theEvent);
            var r = SCNQuaternion.FromAxisAngle (new SCNVector3 (0, 0, 1), -theEvent.Rotation / 180);
            r.Normalize ();
            Trackball.Rotate (r);
        }

        public override void KeyDown (NSEvent theEvent)
        {
            switch (theEvent.KeyCode) {
            case (ushort)NSKey.UpArrow when theEvent.ModifierFlags.HasFlag (NSEventModifierMask.ShiftKeyMask):
                Trackball.PanUp ();
                break;
            case (ushort)NSKey.DownArrow when theEvent.ModifierFlags.HasFlag (NSEventModifierMask.ShiftKeyMask):
                Trackball.PanDown ();
                break;
            case (ushort)NSKey.LeftArrow when theEvent.ModifierFlags.HasFlag (NSEventModifierMask.ShiftKeyMask):
                Trackball.PanLeft ();
                break;
            case (ushort)NSKey.RightArrow when theEvent.ModifierFlags.HasFlag (NSEventModifierMask.ShiftKeyMask):
                Trackball.PanRight ();
                break;
            case (ushort)NSKey.UpArrow:
                Trackball.RotateUp ();
                break;
            case (ushort)NSKey.DownArrow:
                Trackball.RotateDown ();
                break;
            case (ushort)NSKey.LeftArrow:
                Trackball.RotateLeft ();
                break;
            case (ushort)NSKey.RightArrow:
                Trackball.RotateRight ();
                break;
            case (ushort)NSKey.Escape:
                Trackball.Reset ();
                break;
            default:
                base.KeyDown (theEvent);
                break;
            }
        }
    }
}