//
// ReflectionRemotingRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;

using Xamarin.CrossBrowser;

using Node = Xamarin.Interactive.Representations.Reflection.Node;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof (Node), false)]
	sealed class ReflectionRemotingRenderer : HtmlRendererBase
	{
		public override string CssClass => null;

		Node node;

		protected override void HandleBind () => node = (Node)RenderState.Source;

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return new RendererRepresentation (
				node.GetType ().Name, options: RendererRepresentationOptions.ForceExpand);
		}

		protected override void HandleRender (RenderTarget target)
		{
			var writer = new StringWriter ();
			var renderer = new CSharpTextRenderer (writer);
			node.AcceptVisitor (renderer);
			target.InlineTarget.AppendChild (
				Document.CreateElement ("code", innerHtml: writer.ToString ()));
		}
	}
}