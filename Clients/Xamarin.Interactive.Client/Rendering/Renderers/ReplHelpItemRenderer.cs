//
// ReplHelpItemRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections.Generic;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof (ReplHelp.Item))]
	sealed class ReplHelpItemRenderer : HtmlRendererBase
	{
		public override string CssClass => "renderer-help-item";

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return new RendererRepresentation (
				"Help Item", options: RendererRepresentationOptions.ForceExpand);
		}

		protected override void HandleRender (RenderTarget target)
			=> Context.Render (RenderState.CreateChild (
				new ReplHelp { (ReplHelp.Item)RenderState.Source }),
				target.ExpandedTarget);
	}
}