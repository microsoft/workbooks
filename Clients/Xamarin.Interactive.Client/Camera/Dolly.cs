//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Xamarin.Interactive.Camera
{
    interface IDolly
    {
        void Reset ();
    }

    abstract class Dolly<TQuaternion, TPoint> : IDolly
    {
        Quaternion around = Quaternion.Identity;
        public Quaternion Around {
            get => around;
            set => around = value;
        }

        Quaternion orientation = Quaternion.Identity;
        protected Quaternion Orientation {
            get => orientation;
            set => SetOrientation (value);
        }

        protected virtual void SetOrientation (Quaternion o)
            => orientation = Clamp (o);

        const double MinScale = 0.1;
        const double MaxScale = 10;

        Vector3 scale = new Vector3 { X = 1, Y = 1, Z = 1 };
        protected Vector3 Scale {
            get => scale;
            set => SetScale (value);
        }

        protected virtual void SetScale (Vector3 s)
            => scale = ClampScale (s);

        Vector3 position = new Vector3 ();
        protected Vector3 Position {
            get => position;
            set => SetPosition (value);
        }

        protected virtual void SetPosition (Vector3 p)
            => position = p;

        public bool FlipY { get; set; } = false;
        public bool CameraLocal { get; set; } = false;

        Vector2 previousLocation;
        Vector3 previousDirection;

        public void StartDrag (TPoint p, float width, float height)
            => previousDirection = ProjectToSphere (
                previousLocation = Convert (p),
                width,
                height);

        const float DefaultRotate = (float)(Math.PI * .05);

        public void RotateLeft (float angle = DefaultRotate)
            => Rotate (Quaternion.CreateFromAxisAngle (new Vector3 (0, 1, 0), -angle));

        public void RotateRight (float angle = DefaultRotate)
            => Rotate (Quaternion.CreateFromAxisAngle (new Vector3 (0, 1, 0), angle));

        public void RotateUp (float angle = DefaultRotate)
            => Rotate (Quaternion.CreateFromAxisAngle (new Vector3 (1, 0, 0), -angle));

        public void RotateDown (float angle = DefaultRotate)
            => Rotate (Quaternion.CreateFromAxisAngle (new Vector3 (1, 0, 0), angle));

        const float DefaultPan = 0.1f;

        public void PanLeft ()
            => Pan (new Vector3 (-DefaultPan, 0, 0));

        public void PanRight ()
            => Pan (new Vector3 (DefaultPan, 0, 0));

        public void PanUp ()
            => Pan (new Vector3 (0, DefaultPan, 0));

        public void PanDown ()
            => Pan (new Vector3 (0, -DefaultPan, 0));

        public void DragRotate (TPoint p, float width, float height)
        {
            var direction = ProjectToSphere (previousLocation = Convert (p), width, height);
            if ((direction - previousDirection).LengthSquared () < 0.001)
                return;

            var rotation = RotationBetween (previousDirection, direction);
            Rotate (rotation);
            previousDirection = direction;
        }

        public void DragZoom (TPoint p, float width, float height)
        {
            var location = Convert (p);
            var delta = location - previousLocation;

            if (delta.LengthSquared () < 0.001)
                return;

            float zoom = (float)Math.Exp (delta.Y * .01);
            Scale *= zoom;
            previousLocation = location;
        }

        public void DragPan (TPoint p, float width, float height)
        {
            var location = Convert (p);
            var delta = location - previousLocation;

            if (delta.LengthSquared () < 0.001)
                return;

            if (FlipY)
                delta.Y = -delta.Y;

            Pan (new Vector3 { X = delta.X / width, Y = delta.Y / height, Z = 0 });
            previousLocation = location;
        }

        protected Vector3 ClampScale (Vector3 s) =>
             VectorUtilities.ClampComponents (s, MinScale, MaxScale);

        protected virtual Quaternion Clamp (Quaternion q)
        {
            if (around.W == 0)
                return Quaternion.Identity;

            if (around.W != 1) {
                // Rotation is locked around this axis
                // try to do something sane... maybe
                VectorUtilities.ToAxisAngle (around, out var aroundAxis, out var aroundAngle);
                VectorUtilities.ToAxisAngle (q, out var rotationAxis, out var rotationAngle);

                var scale = Vector3.Dot (aroundAxis, rotationAxis);
                var a = VectorUtilities.Clamp (
                    rotationAngle * scale,
                    -Math.Abs (aroundAngle),
                    Math.Abs (aroundAngle));

                q = Quaternion.CreateFromAxisAngle (aroundAxis, a);
            }

            return Quaternion.Normalize (q);
        }

        protected Vector3 ProjectToSphere (Vector2 location, float width, float height)
        {
            var x = location.X / (width * 0.5f);
            var y = location.Y / (height * 0.5f);

            // limit the backside projections to 90 degrees to avoid
            // over rotating in one motion
            x = x - 1;
            y = FlipY ? 1 - y : y - 1;

            var z2 = 1 - x * x - y * y;
            var z = z2 > 0 ? Math.Sqrt (z2) : -Math.Sqrt (-z2);
            return Vector3.Normalize (new Vector3 (x, y, (float)z));
        }

        protected Quaternion RotationBetween (Vector3 v1, Vector3 v2)
        {
            var axis = Vector3.Cross (v1, v2);
            var w = Math.Sqrt (v1.LengthSquared () * v2.LengthSquared ()) + Vector3.Dot (v1, v2);
            return Quaternion.Normalize (new Quaternion (axis.X, axis.Y, axis.Z, (float)w));
        }

        public void Rotate (Quaternion rotation)
            => Orientation = Quaternion.Normalize (rotation * Orientation);

        public void Pan (Vector3 offset)
            => Position += offset;

        public void Zoom (Vector3 zoom)
            => Scale += zoom;

        public abstract Quaternion Convert (TQuaternion platform);
        public abstract TQuaternion Convert (Quaternion q);
        public abstract Vector2 Convert (TPoint point);

        const double AnimationDuration = 0.5;

        public void Reset (Action completed = null, double duration = AnimationDuration)
            => Reset (Quaternion.Identity,
                   new Vector3 (),
                   new Vector3 (1, 1, 1),
                   completed,
                   duration);

        public virtual void Reset (
            Quaternion orientation,
            Vector3 position,
            Vector3 scale,
            Action completed = null,
            double duration = AnimationDuration)
        {
            SetOrientation (orientation);
            SetPosition (position);
            SetScale (scale);
            Around = around;
            completed?.Invoke ();
        }

        readonly Stack<Settings> settings = new Stack<Settings> ();

        public void PushSettings ()
        {
            settings.Push (new Settings {
                Orientation = Orientation,
                Scale = Scale,
                Position = Position,
                Around = Around
            });
        }

        public void PopSettings (Action completed = null, double duration = AnimationDuration)
        {
            if (settings.Count > 0) {
                var vals = settings.Pop ();
                Around = vals.Around;
                Reset (
                    vals.Orientation,
                    vals.Position,
                    vals.Scale,
                    completed,
                    duration
                );
            }
        }

        void IDolly.Reset () =>
            Reset ();

        sealed class Settings
        {
            public Quaternion Orientation;
            public Vector3 Scale;
            public Vector3 Position;
            public Quaternion Around;
        }
    }
}