//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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