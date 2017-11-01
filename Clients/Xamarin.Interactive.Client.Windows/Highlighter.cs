// AgentSessionWindow.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

using Xamarin.Interactive.Client.Windows.Views;

namespace Xamarin.Interactive.Client.Windows
{
    class HighlightSelectionEventArgs : EventArgs
    {
        public InspectView SelectedView
        { get; set; }
    }

    sealed class Highlighter : IDisposable
    {
        const string TAG = "Highlighter";

        readonly MouseHookListener mouseListener;
        readonly DispatcherTimer mouseMoveThrottlingTimer;
        ClientSession clientSession;
        MouseEventExtArgs lastIgnoredMouseArgs;
        bool throttlingMouseMove;
        CancellationTokenSource highlightOnMoveCts;
        AgentCoordinateMapper coordinateMapper;
        ViewHighlightOverlayWindow overlayWindow;
        InspectView highlightedView;
        bool isDisposed;
        string hierarchyKind;

        public event EventHandler HighlightEnded;

        public event EventHandler<HighlightSelectionEventArgs> ViewSelected;

        public Highlighter ()
        {
            mouseListener = new MouseHookListener (new GlobalHooker ());
            mouseListener.MouseMoveExt += OnGlobalMouseMoveExt;
            mouseListener.MouseDownExt += OnGlobalMouseDown;

            mouseMoveThrottlingTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds (50),
            };
            mouseMoveThrottlingTimer.Tick += MouseMoveThrottlingTimerOnTick;
        }

        public void Dispose ()
        {
            isDisposed = true;
            mouseMoveThrottlingTimer.IsEnabled = false;

            mouseListener.Enabled = false;
            mouseListener.MouseMoveExt -= OnGlobalMouseMoveExt;
            mouseListener.MouseDownExt -= OnGlobalMouseMoveExt;
            mouseListener.Dispose ();

            CancelHighlightOnMove ();

            overlayWindow?.Close ();
        }

        public void Start (ClientSession clientSession, Window parentWindow, string hierarchyKind)
        {
            if (mouseListener.Enabled)
                return;

            this.clientSession = clientSession;
            this.hierarchyKind = hierarchyKind;

            if (overlayWindow == null) {
                overlayWindow = new ViewHighlightOverlayWindow {
                    Owner = parentWindow,
                    ShowActivated = true,
                    WindowState = WindowState.Maximized,
                };
                // Prevent closing by user (with alt+F4, for example)
                overlayWindow.Closing += (o, a) => a.Cancel = !isDisposed;
            }
            overlayWindow.Show ();

            CancelHighlightOnMove ();
            highlightOnMoveCts = new CancellationTokenSource ();

            coordinateMapper = AgentCoordinateMapper.Create (clientSession.Agent.Identity, parentWindow);
            mouseListener.Enabled = true;
        }

        async void OnGlobalMouseDown (object sender, MouseEventExtArgs eventArgs)
        {
            // UIAutomation and the WpfTap debugger can cause
            // a native exception if we try to hide the 
            // overlayWindow inside the event handler
            // while using the Visual Studio wpf
            // debugger so we Yield to avoid the problem.
            await Task.Yield ();

            eventArgs.Handled = true;
            mouseListener.Enabled = false;

            HighlightEnded?.Invoke (this, EventArgs.Empty);

            overlayWindow?.Hide ();

            try {
                // Since everything's async, it's possible for the move handler's HighlightView call
                // to hit after this, causing the view to stay highlighted. So we cancel.
                CancelHighlightOnMove ();
                await HighlightView (ToPoint (eventArgs.Location), andSelect: true, hierarchyKind: hierarchyKind);
            } catch (Exception e) {
                Log.Error (TAG, e);
            }
        }

        async void OnGlobalMouseMoveExt (object sender, MouseEventExtArgs mouseEventExtArgs)
        {
            if (!mouseListener.Enabled)
                return;

            if (throttlingMouseMove) {
                lastIgnoredMouseArgs = mouseEventExtArgs;
                return;
            }

            lastIgnoredMouseArgs = null;
            throttlingMouseMove = true;

            // TODO: If mouse has moved to another screen, move overlayWindow.

            try {
                await HighlightView (
                    ToPoint (mouseEventExtArgs.Location),
                    andSelect: false,
                    hierarchyKind: hierarchyKind,
                    cancellationToken: highlightOnMoveCts.Token);
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            mouseMoveThrottlingTimer.IsEnabled = true;
        }

        void MouseMoveThrottlingTimerOnTick (object sender, EventArgs eventArgs)
        {
            mouseMoveThrottlingTimer.IsEnabled = false;

            throttlingMouseMove = false;

            if (lastIgnoredMouseArgs != null)
                OnGlobalMouseMoveExt (null, lastIgnoredMouseArgs);
        }

        void CancelHighlightOnMove ()
        {
            if (highlightOnMoveCts == null)
                return;

            highlightOnMoveCts.Cancel ();
            highlightOnMoveCts.Dispose ();
            highlightOnMoveCts = null;
        }

        async Task HighlightView (
            Point screenPt,
            bool andSelect,
            string hierarchyKind,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            Point devicePt;
            InspectView view = null;

            if (coordinateMapper != null && coordinateMapper.TryGetLocalCoordinate (screenPt, out devicePt)) {
                view = await clientSession.Agent.Api.HighlightView<InspectView> (
                    devicePt.X,
                    devicePt.Y,
                    clear: true,
                    hierarchyKind: hierarchyKind,
                    cancellationToken: cancellationToken);
            }

            if (view == null) {
                highlightedView = null;
                overlayWindow.Clear ();
                return;
            }

            // Don't clear or redraw anything. We are over the same view.
            if (highlightedView != null && view.Handle == highlightedView.Handle && !andSelect)
                return;

            highlightedView = view;
            overlayWindow.Clear ();

            if (andSelect) {
                highlightedView = null;
                ViewSelected?.Invoke (this, new HighlightSelectionEventArgs {SelectedView = view});
            } else
                overlayWindow.HighlightRect (coordinateMapper.GetHostRect (new Rect {
                    X = view.X,
                    Y = view.Y,
                    Width = view.Width,
                    Height = view.Height,
                }));
        }

        static Point ToPoint (System.Drawing.Point point)
        {
            return new Point (point.X, point.Y);
        }
    }
}