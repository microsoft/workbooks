//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Rendering
{
    sealed class JavaScriptRendererRepresentation : RendererRepresentation
    {
        public dynamic JSPeer { get; }

        public JavaScriptRendererRepresentation (dynamic jsPeer) : base (
            jsPeer?.shortDisplayName as string,
            jsPeer?.HasProperty ("state")
                ? (object)jsPeer.state
                : null,
            jsPeer?.HasProperty ("options")
                ? (RendererRepresentationOptions)jsPeer.options
                : RendererRepresentationOptions.None,
            jsPeer?.HasProperty ("order")
                ? (int)jsPeer.order
                : 0)
        {
            if (jsPeer == null)
                throw new ArgumentNullException (nameof (jsPeer));

            JSPeer = jsPeer;
        }
    }
}