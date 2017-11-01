//
// RenderTarget.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using Xamarin.CrossBrowser;

namespace Xamarin.Interactive.Rendering
{
	struct RenderTarget
	{
		public RendererRepresentation Representation { get; }
		public HtmlElement InlineTarget { get; }
		public HtmlElement ExpandedTarget { get; }

		public bool IsExpanded => ExpandedTarget != null;

		public RenderTarget (
			RendererRepresentation representation,
			HtmlElement inlineTarget,
			HtmlElement expandedTarget)
		{
			Representation = representation;
			InlineTarget = inlineTarget;
			ExpandedTarget = expandedTarget;
		}
	}
}