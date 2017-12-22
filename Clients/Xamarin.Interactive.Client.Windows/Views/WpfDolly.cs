//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

using Xamarin.Interactive.Camera;
using Xamarin.Interactive.Client.ViewInspector;
using Xamarin.Interactive.Client.Windows.Commands;

using SN = System.Numerics;

namespace Xamarin.Interactive.Client.Windows.Views
{
    sealed class WpfDolly : Dolly<Quaternion, Point>
    {
        readonly TranslateTransform3D position = new TranslateTransform3D ();
        readonly ScaleTransform3D scale = new ScaleTransform3D ();
        readonly QuaternionRotation3D rotation = new QuaternionRotation3D {
            Quaternion = Quaternion.Identity
        };

        public Transform3DGroup Transform { get; }

        public DelegateCommand RotateLeftCommand { get; }
        public DelegateCommand RotateRightCommand { get; }
        public DelegateCommand RotateUpCommand { get; }
        public DelegateCommand RotateDownCommand { get; }
        public DelegateCommand ResetCommand { get; }

        public WpfDolly ()
        {
            CameraLocal = true;
            FlipY = true;

            Transform = new Transform3DGroup ();
            Transform.Children.Add (scale);
            Transform.Children.Add (position);
            Transform.Children.Add (new RotateTransform3D (rotation));

            Around = SN.Quaternion.CreateFromAxisAngle (
                SN.Vector3.Normalize (new SN.Vector3 (0, 1, 0)),
                (float)(Math.PI * .45));

            float ParameterToAngle (object arg)
            {
                switch (arg) {
                case int amount:
                    return amount;
                case string s when double.TryParse (s, out var d):
                    return (float)d;
                default:
                    return (float)(Math.PI * .05);
                }
            }

            RotateLeftCommand = new DelegateCommand (
                param => RotateLeft (ParameterToAngle (param)));

            RotateRightCommand = new DelegateCommand (
                param => RotateRight (ParameterToAngle (param)));

            RotateUpCommand = new DelegateCommand (
                param => RotateUp (ParameterToAngle (param)));

            RotateDownCommand = new DelegateCommand (
                param => RotateDown (ParameterToAngle (param)));

            ResetCommand = new DelegateCommand (
                param => Reset ());
        }

        #region WPF/System.Numerics Conversions 

        public override SN.Quaternion Convert (Quaternion pq)
            => new SN.Quaternion ((float)pq.X, (float)pq.Y, (float)pq.Z, (float)pq.W);

        public override Quaternion Convert (SN.Quaternion q)
            => new Quaternion (q.X, q.Y, q.Z, q.W);

        public override SN.Vector2 Convert (Point point)
            => new SN.Vector2 ((float)point.X, (float)point.Y);

        #endregion

        #region WPF Implementations of Orientation/Position/Scale

        public override void Reset (
            SN.Quaternion orientation,
            SN.Vector3 position,
            SN.Vector3 scale,
            Action completed = null,
            double duration = 1.0)
        {
            if (duration <= 0) {
                base.Reset (orientation, position, scale, completed, duration);
                return;
            }

            var span = TimeSpan.FromSeconds (duration);
            SetOrientation (orientation, span, completed);
            SetPosition (position, span);
            SetScale (scale, span);
        }

        protected override void SetOrientation (SN.Quaternion o)
            => SetOrientation (o, TimeSpan.Zero);

        void SetOrientation (SN.Quaternion o, TimeSpan duration, Action completed = null)
        {
            o = Clamp (o);

            var targetOrientation = CameraLocal
                ? SN.Quaternion.Inverse (o)
                : o;

            if (duration.Ticks > 0) {
                Animate (
                    rotation,
                    QuaternionRotation3D.QuaternionProperty,
                    new QuaternionAnimation {
                        To = Convert (targetOrientation),
                        Duration = duration,
                        EasingFunction = new CubicEase (),
                    },
                    completed
                );
                base.SetOrientation (o);
            } else {
                base.SetOrientation (o);
                rotation.Quaternion = Convert (targetOrientation);
                completed?.Invoke ();
            }
        }

        protected override void SetPosition (SN.Vector3 p)
            => SetPosition (p, TimeSpan.Zero);

        void SetPosition (SN.Vector3 p, TimeSpan duration)
        {
            var targetPosition = p;

            AnimateFromVector3 (
                position,
                -targetPosition,
                duration,
                TranslateTransform3D.OffsetXProperty,
                TranslateTransform3D.OffsetYProperty,
                TranslateTransform3D.OffsetZProperty);

            base.SetPosition (p);
        }

        protected override void SetScale (SN.Vector3 s)
            => SetScale (s, TimeSpan.Zero);

        void SetScale (SN.Vector3 s, TimeSpan duration)
        {
            s = ClampScale (s);
            AnimateFromVector3 (
                scale,
                s,
                duration,
                ScaleTransform3D.ScaleXProperty,
                ScaleTransform3D.ScaleYProperty,
                ScaleTransform3D.ScaleZProperty);

            base.SetScale (s);
        }

        #endregion

        #region Event Handling

        FrameworkElement eventSource;
        public FrameworkElement EventSource {
            get => eventSource;
            set {
                if (eventSource != null) {
                    eventSource.KeyDown -= EventSource_KeyDown;
                    eventSource.MouseDown -= EventSource_MouseDown;
                    eventSource.MouseUp -= EventSource_MouseUp;
                    eventSource.MouseMove -= EventSource_MouseMove;
                    eventSource.MouseWheel -= EventSource_MouseWheel;
                }

                eventSource = value;

                if (eventSource != null) {
                    eventSource.KeyDown += EventSource_KeyDown;
                    eventSource.MouseDown += EventSource_MouseDown;
                    eventSource.MouseUp += EventSource_MouseUp;
                    eventSource.MouseMove += EventSource_MouseMove;
                    eventSource.MouseWheel += EventSource_MouseWheel;
                }
            }
        }

        void EventSource_KeyDown (object sender, KeyEventArgs e)
        {
            var handled = true;

            switch (e.Key) {
            case Key.Left when Keyboard.Modifiers.HasFlag (ModifierKeys.Shift):
                PanLeft ();
                break;
            case Key.Right when Keyboard.Modifiers.HasFlag (ModifierKeys.Shift):
                PanRight ();
                break;
            case Key.Up when Keyboard.Modifiers.HasFlag (ModifierKeys.Shift):
                PanUp ();
                break;
            case Key.Down when Keyboard.Modifiers.HasFlag (ModifierKeys.Shift):
                PanDown ();
                break;
            case Key.Left:
                RotateLeft ();
                break;
            case Key.Right:
                RotateRight ();
                break;
            case Key.Up:
                RotateUp ();
                break;
            case Key.Down:
                RotateDown ();
                break;
            case Key.Escape:
                Reset ();
                break;
            default:
                handled = false;
                break;
            }

            e.Handled = handled;
        }

        void EventSource_MouseDown (object sender, MouseEventArgs e)
        {
            // Initialize the positions first because
            // Mouse.Capture will generate a motion event
            StartDrag (
                e.GetPosition (EventSource),
                (float)EventSource.ActualWidth,
                (float)EventSource.ActualHeight);

            if (!EventSource.IsFocused)
                EventSource.Focus ();

            Mouse.Capture (EventSource, CaptureMode.Element);

            e.Handled = true;
        }

        void EventSource_MouseUp (object sender, MouseEventArgs e)
            => Mouse.Capture (EventSource, CaptureMode.None);

        void EventSource_MouseMove (object sender, MouseEventArgs e)
        {
            var currentPosition = e.GetPosition (EventSource);

            // Prefer tracking to zooming if both buttons are pressed.
            if (e.LeftButton == MouseButtonState.Pressed && (Keyboard.Modifiers == ModifierKeys.None)) {
                DragRotate (currentPosition, (float)EventSource.ActualWidth, (float)EventSource.ActualHeight);
            } else if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers.HasFlag (ModifierKeys.Shift)
                || e.RightButton == MouseButtonState.Pressed && (Keyboard.Modifiers == ModifierKeys.None)) {
                DragPan (currentPosition, (float)EventSource.ActualWidth, (float)EventSource.ActualHeight);
            } else if (e.MiddleButton == MouseButtonState.Pressed
                || (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers.HasFlag (ModifierKeys.Control))) {
                DragZoom (currentPosition, (float)EventSource.ActualWidth, (float)EventSource.ActualHeight);
            }
        }

        void EventSource_MouseWheel (object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag (ModifierKeys.Control)) {
                var zoom = -(float)(e.Delta * .001);
                Zoom (new SN.Vector3 (zoom, zoom, zoom));
            }
        }

        #endregion

        #region Animation Helpers

        static void Animate (
            Animatable dep,
            DependencyProperty dp,
            AnimationTimeline timeline,
            Action completed = null)
        {
            timeline.Completed += CompletionHandler;
            dep.BeginAnimation (dp, timeline);

            void CompletionHandler (object sender, EventArgs e)
            {
                timeline.Completed -= CompletionHandler;

                var value = dep.GetValue (dp);
                dep.BeginAnimation (dp, null);
                dep.SetValue (dp, value);
                completed?.Invoke ();
            }
        }

        static void AnimateFromVector3 (
            Animatable animatable,
            SN.Vector3 to,
            TimeSpan duration,
            DependencyProperty xProperty,
            DependencyProperty yProperty,
            DependencyProperty zProperty)
        {
            if (duration.Ticks <= 0) {
                animatable.SetValue (xProperty, (double)to.X);
                animatable.SetValue (yProperty, (double)to.Y);
                animatable.SetValue (zProperty, (double)to.Z);
                return;
            }

            Animate (animatable, xProperty, new DoubleAnimation {
                To = to.X,
                Duration = duration,
                EasingFunction = new CubicEase ()
            });

            Animate (animatable, yProperty, new DoubleAnimation {
                To = to.Y,
                Duration = duration,
                EasingFunction = new CubicEase ()
            });

            Animate (animatable, zProperty, new DoubleAnimation {
                To = to.Z,
                Duration = duration,
                EasingFunction = new CubicEase ()
            });
        }

        #endregion
    }
}