//
// iOSSimulatorCoordinateMapper.cs
//
// Author:
//   Miguel de Icaza
//
// Copyright 2013-2014 Xamarin Inc.

using System;
using System.Collections.Immutable;
using System.Linq;

using CoreGraphics;
using Foundation;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Mac.CoordinateMappers
{
    class iOSSimulatorCoordinateMapper : AgentCoordinateMapper
    {
        const string TAG = nameof (iOSSimulatorCoordinateMapper);

        readonly CGRect appBounds;
        readonly nfloat scale;

        public iOSSimulatorCoordinateMapper (AgentIdentity agentIdentity)
        {
            // TODO: Pick the correct window with extra information available when Device Switching work
            //       lands. For now, we know we always launch 5s, but in Xcode 9 there may be multiple
            //       sims open simultaneously.
            var window = InspectableWindow
                .GetWindows ("com.apple.iphonesimulator")
                .FirstOrDefault (w => w.Title != null && w.Title.Contains ("5s"));

            if (window == null) {
                Log.Error (TAG, "Unable to locate simulator window");
                return;
            }

            if (!ShowDeviceBezels (window)) {
                appBounds = window.Bounds;
                appBounds.Y += 22;
                appBounds.Height -= 22;
            } else {
                var shape = GetDeviceShape (window);
                var chromeScale = window.Bounds.Height / GetDeviceFullHeight (shape);
                var verticalMargin = GetDeviceVerticalMargin (shape) * chromeScale;
                var horizontalMargin = GetDeviceHorizontalMargin (shape) * chromeScale;

                appBounds = new CGRect {
                    X = window.Bounds.X + horizontalMargin,
                    Y = window.Bounds.Y + verticalMargin,
                    Width = window.Bounds.Width - (horizontalMargin * 2),
                    Height = window.Bounds.Height - (verticalMargin * 2),
                };
            }

            scale = agentIdentity.ScreenWidth / appBounds.Width;
        }

        public override bool TryGetLocalCoordinate (CGPoint hostCoordinate, out CGPoint localCoordinate)
        {
            localCoordinate = CGPoint.Empty;

            if (!appBounds.Contains (hostCoordinate))
                return false;

            localCoordinate.X = (hostCoordinate.X - appBounds.X) * scale;
            localCoordinate.Y = (hostCoordinate.Y - appBounds.Y) * scale;

            return true;
        }

        // TODO: Replace this when Device Switching work lands, and we know from the AgentProcess what simulator
        //       type is being used.
        static DeviceShape GetDeviceShape (InspectableWindow window)
        {
            if (window.Title.Contains ("5s") || window.Title.Contains ("SE"))
                return DeviceShape.IPhone5;
            else if (window.Title.Contains ("X"))
                return DeviceShape.IPhoneX;
            else if (window.Title.Contains ("Plus"))
                return DeviceShape.IPhoneStandardPlus;
            else
                return DeviceShape.IPhoneStandard;
        }

        /// <summary>
		/// Get the height of the entire simulator window when at 100% scaling,
		/// assuming Xcode 9 sim with bezels enabled.
		/// </summary>
        static int GetDeviceFullHeight (DeviceShape shape)
        {
            switch (shape) {
            case DeviceShape.IPhoneStandard:
                return 869;
            case DeviceShape.IPhoneStandardPlus:
                return 938;
            case DeviceShape.IPhoneX:
                return 852;
            case DeviceShape.IPhone5:
            default:
                return 770;
            }
        }

        /// <summary>
		/// Get the vertical margin (same on top and bottom) for the actual screen
		/// region of the simulator window when at 100% scaling with Xcode 9 bezels enabled.
		/// </summary>
        static int GetDeviceVerticalMargin (DeviceShape shape)
        {
            switch (shape) {
            case DeviceShape.IPhoneX:
                return 20;
            default:
                return 101;
            }
        }

        /// <summary>
		/// Get the horizontal margin (same on left and right) for the actual screen
		/// region of the simulator window when at 100% scaling with Xcode 9 bezels enabled.
		/// </summary>
        static int GetDeviceHorizontalMargin (DeviceShape shape)
        {
            switch (shape) {
            case DeviceShape.IPhoneX:
                return 29;
            default:
                return 36;
            }
        }

        static bool ShowDeviceBezels (InspectableWindow window)
        {
            var simDefaults = new NSUserDefaults (
                "com.apple.iphonesimulator",
                NSUserDefaultsType.SuiteName);

            const string ShowChromeSetting = "ShowChrome";

            return simDefaults.ValueForKey (new NSString (ShowChromeSetting)) == null
                ? true
                : simDefaults.BoolForKey (ShowChromeSetting);
        }

        // Different simulator device shapes when bezels are enabled
        enum DeviceShape
        {
            IPhone5 = 0,
            IPhoneStandard,
            IPhoneStandardPlus,
            IPhoneX,
        }
    }
}