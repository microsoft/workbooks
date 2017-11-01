//
// ExceptionRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Collections.Generic;

using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Representations.Reflection;

using Xamarin.CrossBrowser;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof(Exception), false)]
	sealed class ExceptionRenderer : HtmlRendererBase
	{
		public override string CssClass => "renderer-exception";

		string message;
		ExceptionNode exception;

		protected override void HandleBind ()
		{
			var xipException = RenderState.Source as XipErrorMessageException;
			if (xipException != null) {
				message = xipException.XipErrorMessage.Message;
				exception = xipException.XipErrorMessage.Exception;
			} else {
				var e = (Exception)RenderState.Source;
				message = e.Message;
				exception = ExceptionNode.Create (e);
			}
		}

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return new RendererRepresentation (
				"Exception", options: RendererRepresentationOptions.ForceExpand);
		}

		protected override void HandleRender (RenderTarget target)
		{
			var writer = new StringWriter ();
			var renderer = new CSharpTextRenderer (writer);
			exception.AcceptVisitor (renderer);
			target.InlineTarget.AppendTextNode (message);
			target.ExpandedTarget.InnerHTML = writer.ToString ();
		}
	}
}