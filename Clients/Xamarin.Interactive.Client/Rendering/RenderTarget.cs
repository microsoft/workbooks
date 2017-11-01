//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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