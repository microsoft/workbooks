//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Numerics;

using CoreGraphics;
using SceneKit;

using Xamarin.Interactive.Camera;

namespace Xamarin.Interactive.Client.Mac.ViewInspector
{
    sealed class SceneKitDolly : Dolly<SCNQuaternion, CGPoint>
    {
        const double AnimationDuration = 1.0;
        const double lockLimitAngle = Math.PI * .5;

        public SCNNode Target { get; set; }

        public SceneKitDolly ()
            => Around = Quaternion.CreateFromAxisAngle (
                new Vector3 (0, 1, 0),
                (float)(Math.PI * .5));

        protected override void SetOrientation (Quaternion o)
        {
            base.SetOrientation (o);

            var targetOrientation = CameraLocal
                ? Quaternion.Inverse (Orientation)
                : Orientation;

            Target.Orientation = Convert (targetOrientation);
        }

        protected override void SetScale (Vector3 s)
        {
            base.SetScale (s);

            Target.Scale = Convert (Scale);
        }

        protected override void SetPosition (Vector3 p)
        {
            base.SetPosition (p);

            Target.Position = Convert (Position);
        }

        public void StartDrag (CGPoint point, CGSize size)
            => StartDrag (point, (float)size.Width, (float)size.Height);

        public void DragRotate (CGPoint point, CGSize size)
            => DragRotate (point, (float)size.Width, (float)size.Height);

        public void Rotate (SCNQuaternion delta)
            => Animate (() => Rotate (Convert (delta)));

        public void Zoom (SCNVector3 zoom)
            => Zoom (new Vector3 ((float)zoom.X, (float)zoom.Y, (float)zoom.Z));

        public override void Reset (
            Quaternion orientation,
            Vector3 position,
            Vector3 scale,
            Action completed = null,
            double duration = AnimationDuration)
            => Animate (() => {
                Orientation = orientation;
                Position = position;
                Scale = scale;
            }, completed, duration);

        void Animate (
            Action action,
            Action completion = null,
            double duration = AnimationDuration,
            bool resetAnimationDuration = true)
        {
            if (duration <= 0) {
                action ();
                completion?.Invoke ();
                return;
            }

            var savedAnimationDuration = SCNTransaction.AnimationDuration;
            SCNTransaction.AnimationDuration = duration;
            SCNTransaction.Begin ();
            if (completion != null)
                SCNTransaction.SetCompletionBlock (completion);

            action ();

            SCNTransaction.Commit ();
            if (resetAnimationDuration)
                SCNTransaction.AnimationDuration = savedAnimationDuration;
        }

        public override Quaternion Convert (SCNQuaternion p)
            => new Quaternion {
                X = (float)p.X,
                Y = (float)p.Y,
                Z = (float)p.Z,
                W = (float)p.W
            };

        public override SCNQuaternion Convert (Quaternion q)
            => new SCNQuaternion {
                X = q.X,
                Y = q.Y,
                Z = q.Z,
                W = q.W
            };

        public override Vector2 Convert (CGPoint point)
            => new Vector2 ((float)point.X, (float)point.Y);

        public SCNVector3 Convert (Vector3 v)
            => new SCNVector3 (v.X, v.Y, v.Z);
    }
}