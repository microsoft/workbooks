//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.CrossBrowser;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
    [Renderer (typeof (InteractiveObject), false)]
    sealed class ToStringRenderer : HtmlRendererBase
    {
        static readonly RendererRepresentation representation = new RendererRepresentation (
            "ToString ()",
            options: RendererRepresentationOptions.SuppressDisplayNameHint,
            order: Int32.MaxValue - 2000); // before InteractiveObjectRenderer's representation

        public override string CssClass => "renderer-tostring";
        public override bool CanExpand => false;

        InteractiveObject source;

        protected override void HandleBind () => source = (InteractiveObject)RenderState.Source;

        protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
        {
            if (source != null &&
                !source.SuppressToStringRepresentation &&
                source.RepresentedType != null &&
                source.RepresentedType.ResolvedType != typeof (string))
                yield return representation;
        }

        protected override void HandleRender (RenderTarget target)
            => Context.Render (
                RenderState.CreateChild (source.ToStringRepresentation),
                target.InlineTarget);
    }
}