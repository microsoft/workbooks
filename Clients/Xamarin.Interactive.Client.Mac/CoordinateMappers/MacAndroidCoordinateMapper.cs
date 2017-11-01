//
// MacXapCoordinateMapper.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using CoreGraphics;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.Mac.CoordinateMappers
{
    sealed class MacAndroidCoordinateMapper : AgentCoordinateMapper
    {
        readonly CGRect hostRect;
        readonly nfloat scale;

        public MacAndroidCoordinateMapper (AgentIdentity agentIdentity)
        {
            hostRect = agentIdentity.DeviceManufacturer == "Xamarin"
                ? GetXapHostRect ()
                : GetGoogleHostRect ();

            scale = agentIdentity.ScreenWidth / hostRect.Width;
        }

        static CGRect GetXapHostRect ()
        {
            foreach (var window in InspectableWindow.GetWindows ("com.xamarin.androiddevice",
                onScreenOnly: false)) {
                if (window.Title == null || !window.Title.StartsWith ("Xamarin Android Player - ",
                    StringComparison.Ordinal))
                    continue;

                var xapRect = window.Bounds;
                xapRect.Y += 22;
                xapRect.Height -= 22;
                return xapRect;
            }

            return CGRect.Empty;
        }

        static CGRect GetGoogleHostRect ()
        {
            // TODO: In the future when we can reliably get Android device name, check title for that.
            foreach (var window in InspectableWindow.GetWindowMatchingOwnerName (
                "qemu-system-",
                onScreenOnly: false)) {
                if (window.Title == null || !window.Title.StartsWith ("Android Emulator - ",
                    StringComparison.Ordinal))
                    continue;

                var emuRect = window.Bounds;
                emuRect.Y += 22;
                emuRect.Height -= 22;
                return emuRect;
            }

            return CGRect.Empty;
        }

        public override bool TryGetLocalCoordinate (CGPoint hostCoordinate, out CGPoint localCoordinate)
        {
            localCoordinate = CGPoint.Empty;

            if (!hostRect.Contains (hostCoordinate))
                return false;

            localCoordinate.X = (hostCoordinate.X - hostRect.X) * scale;
            localCoordinate.Y = (hostCoordinate.Y - hostRect.Y) * scale;

            return true;
        }
    }
}