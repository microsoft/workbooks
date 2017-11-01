//
// Author:
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CoreGraphics;
using AppKit;

namespace Xamarin.Interactive.Client.Mac.CoordinateMappers
{
    sealed class MacCoordinateMapper: AgentCoordinateMapper
    {
        public override bool TryGetLocalCoordinate (CGPoint hostCoordinate, out CGPoint localCoordinate)
        {
            // instead of using the hostCoordinate from the CGEvent.Location we will just
            // use the CurrentMouseLocation which is already in the correct screen coordinates.
            var currentMouseLocation = CGPoint.Empty;
            NSApplication.SharedApplication.InvokeOnMainThread (() =>
                currentMouseLocation = NSEvent.CurrentMouseLocation);
            localCoordinate = currentMouseLocation;
            return true;
        }
    }
}