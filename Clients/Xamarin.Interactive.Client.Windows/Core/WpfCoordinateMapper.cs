// WpfCoordinateMapper.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using System.Windows;

namespace Xamarin.Interactive.Core
{
    class WpfCoordinateMapper : AgentCoordinateMapper
    {
        public WpfCoordinateMapper (Window window) { }

        public override bool TryGetLocalCoordinate (Point hostCoordinate, out Point localCoordinate)
        {
            localCoordinate = hostCoordinate;
            return true;
        }

        public override Rect GetHostRect (Rect localRect) => localRect;
    }
}