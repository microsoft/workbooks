//
// IRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Xamarin.Interactive.Rendering
{
    interface IRenderer
    {
        string CssClass { get; }
        bool IsEnabled { get; }
        bool CanExpand { get; }
        RenderState RenderState { get; }

        void Bind (RenderState renderState);
        IEnumerable<RendererRepresentation> GetRepresentations ();
        void Render (RenderTarget target);

        void Expand ();
        void Collapse ();
    }
}