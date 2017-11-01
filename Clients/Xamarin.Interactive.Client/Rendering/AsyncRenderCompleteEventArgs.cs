//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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