//
// AgentCoordinateMapper.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2015 Xamarin Inc. All rights reserved.

using System;

using CoreGraphics;

namespace Xamarin.Interactive.Client.Mac.CoordinateMappers
{
    abstract class AgentCoordinateMapper : IDisposable
    {
        public abstract bool TryGetLocalCoordinate (CGPoint hostCoordinate, out CGPoint localCoordinate);

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
        }
    }
}