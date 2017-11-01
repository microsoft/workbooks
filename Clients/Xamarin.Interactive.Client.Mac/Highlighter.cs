//
// Highlighter.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

using CoreGraphics;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

using Xamarin.Interactive.Client.Mac.CoordinateMappers;

namespace Xamarin.Interactive.Client.Mac
{
	sealed class HighlightSelectionEventArgs : EventArgs
	{
		public InspectView SelectedView
		{ get; set; }
	}

	sealed class Highlighter : IDisposable
	{
		const string TAG = "Highlighter";

		static TimeSpan throttlingTimeout = TimeSpan.FromMilliseconds (50);

		readonly ClientSession clientSession;
		readonly Timer mouseMoveThrottlingTimer;

		bool throttlingMouseMove;
		MouseMonitor mouseMonitor;
		CancellationTokenSource highlightOnMoveCts;
		AgentCoordinateMapper coordinateMapper;
		InspectView highlightedView;
		CGPoint lastIgnoredMouseMovePoint;
		string hierarchyKind;

		public event EventHandler HighlightEnded;

		public event EventHandler<HighlightSelectionEventArgs> ViewSelected;

		public Highlighter (ClientSession clientSession, string hierarchyKind)
		{
			if (clientSession == null)
				throw new ArgumentNullException (nameof (clientSession));

			if (hierarchyKind == null)
				throw new ArgumentNullException (nameof (hierarchyKind));

			this.clientSession = clientSession;
			this.hierarchyKind = hierarchyKind;

			mouseMoveThrottlingTimer = new Timer (
				MouseMoveThrottlingTimerOnTick,
				null,
				throttlingTimeout,
				Timeout.InfiniteTimeSpan);
		}

		public void Dispose ()
		{
			mouseMoveThrottlingTimer.Change (Timeout.Infinite, Timeout.Infinite);
			mouseMoveThrottlingTimer.Dispose ();

			mouseMonitor?.Dispose ();

			CancelHighlightOnMove ();
		}

		public void Start ()
		{
			mouseMonitor?.Dispose ();

			CancelHighlightOnMove ();
			highlightOnMoveCts = new CancellationTokenSource ();

			switch (clientSession.Agent.Type) {
			case AgentType.iOS:
				coordinateMapper = new iOSSimulatorCoordinateMapper (clientSession.Agent.Identity);
				break;
			case AgentType.Android:
				coordinateMapper = new MacAndroidCoordinateMapper (clientSession.Agent.Identity);
				break;
			default:
				coordinateMapper = new MacCoordinateMapper ();
				break;
			}

			mouseMonitor = new MouseMonitor ();
			mouseMonitor.MouseMoved += MouseMonitor_MouseMoved;
			mouseMonitor.MouseUp += MouseMonitor_MouseUp;
		}

		async void MouseMonitor_MouseUp (CGPoint point)
		{
			mouseMonitor.Dispose ();
			mouseMonitor = null;

			HighlightEnded?.Invoke (this, EventArgs.Empty);

			try {
				// Since everything's async, it's possible for the move handler's HighlightView call
				// to hit after this, causing the view to stay highlighted. So we cancel.
				CancelHighlightOnMove ();
				await HighlightView (point, andSelect: true);
			} catch (Exception e) {
				Log.Error (TAG, e);
			}
		}

		async void MouseMonitor_MouseMoved (CGPoint point)
		{
			if (mouseMonitor == null)
				return;

			if (throttlingMouseMove) {
				lastIgnoredMouseMovePoint = point;
				return;
			}

			lastIgnoredMouseMovePoint = CGPoint.Empty;
			throttlingMouseMove = true;

			try {
				await HighlightView (
					point,
					andSelect: false,
					cancellationToken: highlightOnMoveCts.Token);
			} catch (Exception e) {
				Log.Error (TAG, e);
			}

			mouseMoveThrottlingTimer.Change (throttlingTimeout, Timeout.InfiniteTimeSpan);
		}

		void MouseMoveThrottlingTimerOnTick (object state)
		{
			mouseMoveThrottlingTimer.Change (Timeout.Infinite, Timeout.Infinite);

			throttlingMouseMove = false;

			if (!lastIgnoredMouseMovePoint.IsEmpty)
				MouseMonitor_MouseMoved (lastIgnoredMouseMovePoint);
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
			CGPoint screenPt,
			bool andSelect,
			CancellationToken cancellationToken = default (CancellationToken))
		{
			CGPoint devicePt = CGPoint.Empty;
			InspectView view = null;

			var isValidLocalCoordinate = coordinateMapper != null &&
				coordinateMapper.TryGetLocalCoordinate (screenPt, out devicePt);

			view = await clientSession.Agent.Api.HighlightView<InspectView> (
				devicePt.X,
				devicePt.Y,
				clear: andSelect || !isValidLocalCoordinate,
				hierarchyKind: hierarchyKind,
				cancellationToken: cancellationToken);

			if (!isValidLocalCoordinate || view == null) {
				highlightedView = null;
				return;
			}

			// Don't clear or redraw anything. We are over the same view.
			if (highlightedView != null && view.Handle == highlightedView.Handle && !andSelect)
				return;

			highlightedView = view;

			if (andSelect) {
				highlightedView = null;
				ViewSelected?.Invoke (this, new HighlightSelectionEventArgs {SelectedView = view});
			}
		}
	}
}

