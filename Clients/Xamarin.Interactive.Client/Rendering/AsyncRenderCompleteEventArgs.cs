//
// AsyncRenderCompleteEventArgs.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive.Rendering
{
    sealed class AsyncRenderCompleteEventArgs : EventArgs
    {
        public RenderState RenderState { get; }

        public AsyncRenderCompleteEventArgs (RenderState renderState)
        {
            if (renderState == null)
                throw new ArgumentNullException (nameof (renderState));

            RenderState = renderState;
        }
    }
}