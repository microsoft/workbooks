//
// JavaScriptRendererRepresentation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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