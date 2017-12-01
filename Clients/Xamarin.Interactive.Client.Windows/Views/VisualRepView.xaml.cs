//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Client.ViewInspector;

namespace Xamarin.Interactive.Client.Windows.Views
{
    partial class VisualRepView : UserControl
    {
        InspectViewNode currentViewNode;
        InspectViewNode focusedViewNode;
        Point downPosition;

        public VisualRepView ()
        {
            InitializeComponent ();

            CaptureBorder.MouseMove += HandleMouseMove;
            CaptureBorder.MouseDown += HandleMouseDown;
            CaptureBorder.MouseUp += HandleMouseUp;
        }

        void HandleMouseDown (object sender, MouseButtonEventArgs e)
        {
            downPosition = e.GetPosition (CaptureBorder);
        }

        void HandleMouseUp (object sender, MouseButtonEventArgs e)
        {
            var currentPosition = e.GetPosition (CaptureBorder);
            var offset = currentPosition - downPosition;
            if (offset.X == 0 && offset.Y == 0) {
                FocusNode = null;
                VisualTreeHelper.HitTest (
                    viewport,
                    null,
                    ResultCallback,
                    new PointHitTestParameters (currentPosition));
                if (focusedViewNode != null) {
                    SelectedView = FocusNode.InspectView;
                }
            }
        }

        void HandleMouseMove (object sender, MouseEventArgs e)
        {
            var currentPosition = e.GetPosition (CaptureBorder);
            var hitParams = new PointHitTestParameters (currentPosition);
            VisualTreeHelper.HitTest (viewport, null, ResultCallback, hitParams);
        }

        public HitTestResultBehavior ResultCallback (HitTestResult result)
        {
            var meshResult = result as RayMeshGeometry3DHitTestResult;

            if (meshResult != null) {
                var node = meshResult.VisualHit as InspectViewNode;

                FocusNode = node;
                return HitTestResultBehavior.Stop;
            }

            FocusNode = null;
            return HitTestResultBehavior.Continue;
        }

        void OnLoaded (object sender, RoutedEventArgs e)
        {
            // Viewport3Ds only raise events when the mouse is over the rendered 3D geometry.
            // In order to capture events whenever the mouse is over the client are we use a
            // same sized transparent Border positioned on top of the Viewport3D.
            if (Trackball != null)
                Trackball.EventSource = CaptureBorder;
        }

        InspectViewNode FocusNode {
            get { return focusedViewNode; }
            set {
                if (value == focusedViewNode)
                    return;

                focusedViewNode?.Blur ();
                focusedViewNode = value;
                focusedViewNode?.Focus ();
            }
        }

        internal DisplayMode DisplayMode {
            get { return (DisplayMode)GetValue (DisplayModeProperty); }
            set { SetValue (DisplayModeProperty, value); }
        }

        internal bool ShowHidden {
            get { return (bool)GetValue (ShowHiddenProperty); }
            set { SetValue (ShowHiddenProperty, value); }
        }

        public static readonly DependencyProperty SelectedViewProperty =
            DependencyProperty.Register (
                nameof (SelectedView),
                typeof (InspectView),
                typeof (VisualRepView),
                new PropertyMetadata (
                    null,
                    new PropertyChangedCallback (SelectedViewValueChanged)));

        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register (
                nameof (DisplayMode),
                typeof (DisplayMode),
                typeof (VisualRepView),
                new PropertyMetadata (
                    DisplayMode.FramesAndContent,
                    new PropertyChangedCallback (DisplayModeValueChanged)));

        public static readonly DependencyProperty ShowHiddenProperty =
            DependencyProperty.Register (
                nameof (ShowHidden),
                typeof (bool),
                typeof (VisualRepView),
                new PropertyMetadata (
                    false,
                    new PropertyChangedCallback (ShowHiddenValueChanged)));

        static void ShowHiddenValueChanged (DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var value = (bool)eventArgs.NewValue;
            var view = dependencyObject as VisualRepView;
            var state = new InspectTreeState (view.DisplayMode, value);
            view.currentViewNode.Rebuild (state);
        }

        static void DisplayModeValueChanged (DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var value = (DisplayMode) eventArgs.NewValue;
            var view = dependencyObject as VisualRepView;
            var state = new InspectTreeState (value, view.ShowHidden);
            view.currentViewNode?.Rebuild (state);
        }

        static void SelectedViewValueChanged (DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var value = eventArgs.NewValue as InspectView;
            var view = dependencyObject as VisualRepView;

            if (view.FocusNode?.InspectView == value)
                return;

            if (view.currentViewNode != null)
                InspectViewNode.Focus (view.currentViewNode, node => node?.InspectView == value);
        }

        internal InspectView SelectedView
        {
            get { return (InspectView) GetValue (SelectedViewProperty); }
            set { SetValue (SelectedViewProperty, value); }
        }

        public static readonly DependencyProperty CurrentViewProperty =
            DependencyProperty.Register (
                nameof (CurrentView),
                typeof (InspectView),
                typeof (VisualRepView),
                new PropertyMetadata (
                    null,
                    new PropertyChangedCallback (CurrentViewValueChanged)));

        private static void CurrentViewValueChanged (DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var value = eventArgs.NewValue as InspectView;
            var view = dependencyObject as VisualRepView;
            view.currentViewNode = null;
            view.FocusNode = null;
            if (value != null) {
                view.currentViewNode = new InspectViewNode (value, 0).Rebuild (new InspectTreeState (view.DisplayMode, view.ShowHidden));
                view.topModel.Children.Clear ();
                view.topModel.Children.Add (view.currentViewNode);
            }
        }

        internal InspectView CurrentView {
            get { return (InspectView) GetValue (CurrentViewProperty); }
            set { SetValue (CurrentViewProperty, value); }
        }

        public static readonly DependencyProperty TrackballProperty =
            DependencyProperty.Register (
                nameof (Trackball),
                typeof (WpfDolly),
                typeof (VisualRepView),
                new PropertyMetadata (
                    null,
                    new PropertyChangedCallback (TrackballValueChanged)));

        private static void TrackballValueChanged (DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var value = eventArgs.NewValue as WpfDolly;
            var view = dependencyObject as VisualRepView;

            value.EventSource = view.CaptureBorder;
        }

        internal WpfDolly Trackball {
            get { return (WpfDolly)GetValue (TrackballProperty); }
            set { SetValue (TrackballProperty, value); }
        }
    }
}
