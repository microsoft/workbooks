//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
    [Renderer (typeof (ReplHelp))]
    sealed class ReplHelpRenderer : HtmlRendererBase
    {
        public override string CssClass => "renderer-help";

        protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
        {
            yield return new RendererRepresentation (
                "Help", options: RendererRepresentationOptions.ForceExpand);
        }

        protected override void HandleRender (RenderTarget target)
        {
            var help = (ReplHelp)RenderState.Source;

            var tableElem = Document.CreateElement ("table");

            target.ExpandedTarget.AppendChild (tableElem);

            if (help.Items.Count == 1)
                tableElem.AddCssClass ("single-help-item");

            foreach (var item in help) {
                var memberElem = Document.CreateElement ("td");

                // TODO: add a ReflectionNodeRenderer, then do this:
                // Context.Render (item.Member, memberElem);
                var writer = new StringWriter ();
                item.Member.AcceptVisitor (new CSharpTextRenderer (writer) {
                    WriteTypeBeforeMemberName = false,
                    WriteReturnTypes = item.ShowReturnType,
                    WriteMemberTypes = item.ShowReturnType
                });
                memberElem.InnerHTML = writer.ToString ();

                var descriptionElem = Document.CreateElement ("td",
                    innerHtml: item.Description.HtmlEscape ());

                var rowElem = Document.CreateElement ("tr");
                rowElem.AppendChild (memberElem);
                rowElem.AppendChild (descriptionElem);
                tableElem.AppendChild (rowElem);
            }
        }
    }
}