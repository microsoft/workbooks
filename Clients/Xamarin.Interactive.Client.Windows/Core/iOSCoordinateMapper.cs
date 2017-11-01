// iOSCoordinateMapper.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.

using System.Windows;
using System.Windows.Automation;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Core
{
	class iOSCoordinateMapper : AgentCoordinateMapper
	{
		const string TAG = nameof (iOSCoordinateMapper);

		readonly Rect simRect;
		readonly double scale;
		readonly double hostRectScaleFactor;

		public iOSCoordinateMapper (AgentIdentity agentIdentity, Window window)
		{
			// Sometimes, we fail to find the top-level window in the UIAutomation tree. In practice,
			// a single retry appears to fix it. I've rounded up to 3 total tries. --SA
			var tries = 3;
			do {
				simRect = GetSimulatorRect ();
				tries--;
			} while (simRect.IsEmpty && tries > 0);

			// TODO: Better to throw? Or have some IsValid flag that gets set to false? Client shouldn't
			//       proceed with highlight if this happens. Use should be notified that something's up.
			if (simRect.IsEmpty)
				Log.Warning (TAG, "iOS Simulator screen rect not found. Coordinate mapping will fail.");

			hostRectScaleFactor =
				PresentationSource.FromVisual (window).CompositionTarget.TransformToDevice.M11;
			scale = agentIdentity.ScreenWidth / simRect.Width;
		}

		public override bool TryGetLocalCoordinate (Point hostCoordinate, out Point localCoordinate)
		{
			localCoordinate = new Point ();
			if (!simRect.Contains (hostCoordinate))
				return false;

			localCoordinate.X = (hostCoordinate.X - simRect.X) * scale;
			localCoordinate.Y = (hostCoordinate.Y - simRect.Y) * scale;

			return true;
		}

		public override Rect GetHostRect (Rect bounds)
		{
			bounds.Scale (1.0 / scale / hostRectScaleFactor, 1.0 / scale / hostRectScaleFactor);
			bounds.Offset (simRect.X / hostRectScaleFactor, simRect.Y / hostRectScaleFactor);
			return bounds;
		}

		static Rect GetSimulatorRect ()
		{
			var simWindow = GetFirstChild (
				TreeWalker.RawViewWalker,
				AutomationElement.RootElement,
				ControlType.Window,
				ae => ae.Current.AutomationId == "Xamarin.iOS.Simulator.DeviceWindow");

			if (simWindow == null) {
				Log.Warning (TAG, "Top-level iOS Simulator window not found by AutomationId");
				return Rect.Empty;
			}

			var iosScreen = GetFirstChild (
				TreeWalker.RawViewWalker,
				simWindow,
				condition: ae => ae.Current.AutomationId == "Xamarin.iOS.Simulator.DeviceScreen",
				recursive: true);

			if (iosScreen == null) {
				Log.Warning (TAG, "Inner DeviceScreen not found by AutomationId");
				return Rect.Empty;
			}

			return iosScreen.Current.BoundingRectangle;
		}
	}
}
