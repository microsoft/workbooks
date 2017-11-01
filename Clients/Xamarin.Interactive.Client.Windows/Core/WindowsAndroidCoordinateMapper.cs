// WindowsXapCoordinateMapper.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using System;
using System.Windows;
using System.Windows.Automation;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Core
{
    class WindowsAndroidCoordinateMapper : AgentCoordinateMapper
    {
        const string TAG = "WindowsXapCoordinateMapper";

        readonly Rect xapRect;
        readonly double scale;
        readonly double hostRectScaleFactor;

        public WindowsAndroidCoordinateMapper (AgentIdentity agentIdentity, Window window)
        {
            // Sometimes, we fail to find the top-level XAP window in the UIAutomation tree. In practice,
            // a single retry appears to fix it. I've rounded up to 3 total tries. --SA
            var tries = 3;
            do {
                if (agentIdentity.DeviceManufacturer == "Xamarin")
                    xapRect = GetXapBoundingRect ();
                else if (agentIdentity.DeviceManufacturer == "VS Emulator")
                    xapRect = GetVsEmulatorBoundingRect ();
                else
                    xapRect = GetGoogleEmulatorBoundingRect ();
                tries--;
            } while (xapRect.IsEmpty && tries > 0);

            // TODO: Better to throw? Or have some IsValid flag that gets set to false? Client shouldn't
            //       proceed with highlight if this happens. Use should be notified that something's up.
            if (xapRect.IsEmpty)
                Log.Warning (TAG, "XAP screen rect not found. Coordinate mapping will fail.");

            scale = agentIdentity.ScreenWidth / xapRect.Width;
            hostRectScaleFactor =
                PresentationSource.FromVisual (window).CompositionTarget.TransformToDevice.M11;
        }

        public override bool TryGetLocalCoordinate (Point hostCoordinate, out Point localCoordinate)
        {
            localCoordinate = new Point ();

            if (!xapRect.Contains (hostCoordinate))
                return false;

            localCoordinate.X = (hostCoordinate.X - xapRect.X) * scale;
            localCoordinate.Y = (hostCoordinate.Y - xapRect.Y) * scale;

            return true;
        }

        public override Rect GetHostRect (Rect bounds)
        {
            bounds.Scale (1.0 / scale / hostRectScaleFactor, 1.0 / scale / hostRectScaleFactor);
            bounds.Offset (xapRect.X / hostRectScaleFactor, xapRect.Y / hostRectScaleFactor);

            return bounds;
        }

        static Rect GetXapBoundingRect ()
        {
            var xapWindow = GetFirstChild (
                TreeWalker.RawViewWalker,
                AutomationElement.RootElement,
                ControlType.Window,
                ae => ae.Current.Name.StartsWith ("Xamarin Android Player -"));

            if (xapWindow == null) {
                Log.Warning (TAG, "Top-level XAP window not found by name");
                return Rect.Empty;
            }

            var innerWindow = GetFirstChild (
                TreeWalker.RawViewWalker,
                xapWindow,
                ControlType.Window,
                ae => String.IsNullOrEmpty (ae.Current.Name));

            if (innerWindow == null) {
                Log.Warning (TAG, "Inner XAP window not found");
                return Rect.Empty;
            }

            // TODO: In previous tests, this pane did not exist. We should allow for doing math based on
            //       innerWindow in those situations.
            var screenPane = GetFirstChild (
                TreeWalker.RawViewWalker,
                innerWindow,
                ControlType.Pane);

            if (screenPane == null) {
                Log.Warning (TAG, "Inner XAP pane not found");
                return Rect.Empty;
            }

            return screenPane.Current.BoundingRectangle;
        }

        static Rect GetVsEmulatorBoundingRect ()
        {
            var xapWindow = GetFirstChild (
                TreeWalker.RawViewWalker,
                AutomationElement.RootElement,
                ControlType.Window,
                ae => ae.Current.AutomationId.StartsWith ("EmulatorForm"));

            if (xapWindow == null) {
                Log.Warning (TAG, "Top-level VS Emulator window not found by name");
                return Rect.Empty;
            }

            var innerWindow = GetFirstChild (
                TreeWalker.RawViewWalker,
                xapWindow,
                ControlType.Window,
                ae => ae.Current.Name == "XDE");

            if (innerWindow == null) {
                Log.Warning (TAG, "Inner VS Emulator window not found");
                return Rect.Empty;
            }

            return innerWindow.Current.BoundingRectangle;
        }

        static Rect GetGoogleEmulatorBoundingRect (string deviceName = null)
        {
            var window = GetFirstChild (
                TreeWalker.RawViewWalker,
                AutomationElement.RootElement,
                ControlType.Window,
                ae => ae.Current.Name.Contains ($"Android Emulator - {deviceName}"));

            if (window == null) {
                Log.Warning (TAG, "Top-level Google emulator window not found by name");
                return Rect.Empty;
            }

            var pane = GetFirstChild (
                TreeWalker.RawViewWalker,
                window,
                ControlType.Pane,
                ae => ae.Current.Name == "sub");

            if (pane == null) {
                Log.Warning (TAG, "Inner Google emulator pane not found");
                return Rect.Empty;
            }

            return pane.Current.BoundingRectangle;
        }
    }
}