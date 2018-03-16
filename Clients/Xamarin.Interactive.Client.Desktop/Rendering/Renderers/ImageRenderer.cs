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
    [Renderer (typeof (Image))]
    sealed class ImageRenderer : HtmlRendererBase
    {
        public override string CssClass => "renderer-image";

        Image image;

        protected override void HandleBind () => image = (Image)RenderState.Source;

        protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
        {
            yield return new RendererRepresentation (
                "Image", options: RendererRepresentationOptions.ForceExpand, order: -10);
        }

        protected override void HandleRender (RenderTarget target)
        {
            string detailsText = null;
            if (image.Width > 0 && image.Height > 0)
                detailsText = $"{image.Width}×{image.Height} px";

            target.InlineTarget.AppendChild (
                CreateHeaderElement (
                    RenderState.RepresentedType,
                    detailsText));

            if (target.IsExpanded)
                target.ExpandedTarget.AppendChild (RenderImage ());
        }

        HtmlElement RenderImage ()
        {
            var container = Document.CreateElement ("div");

            string mimetype = null;

            switch (image.Format) {
            case ImageFormat.Png:
                mimetype = "image/png";
                break;
            case ImageFormat.Jpeg:
                mimetype = "image/jpeg";
                break;
            case ImageFormat.Gif:
                mimetype = "image/gif";
                break;
            case ImageFormat.Svg:
                mimetype = "image/svg+xml";
                break;
            case ImageFormat.Unknown:
                mimetype = "image";
                break;
            case ImageFormat.Uri:
                break;
            default:
                var errorElem = Document.CreateElement (
                    "div",
                    @class: "renderer-image-error",
                    innerText: $"Unable to render ImageFormat.{image.Format}");
                container.AppendChild (errorElem);
                return container;
            }

            var imageContainer = Document.CreateElement ("div", "renderer-image-container");

            var imageElem = Document.CreateElement ("img");
            if (image.Width > 0)
                imageElem.SetAttribute ("style", $"width: {image.Width}px !important");

            // Right now we only care about image load completion to refocus the input, which is only
            // necessary if the image is the root item, and if we are on Mac, where image loading
            // is fully asynchronous.
            if (RenderState.ParentState == null && HostEnvironment.OS != HostOS.Windows) {
                EventListener onLoadListener = null;
                onLoadListener = imageElem.AddEventListener ("load", evnt => {
                    evnt.Target.RemoveEventListener ("load", onLoadListener);
                    Context.NotifyAsyncRenderComplete (RenderState);
                });
            }

            if (image.Format == ImageFormat.Uri)
                imageElem.SetAttribute ("src",
                    Utf8.GetString (image.Data));
            else
                imageElem.SetAttribute ("src",
                    $"data:{mimetype};base64,{Convert.ToBase64String (image.Data)}");

            imageContainer.AppendChild (imageElem);

            var backgroundImageDiv = Document.CreateElement (
                "div",
                "renderer-image-background-image",
                $"width: {image.Width}px !important;");
            imageContainer.AppendChild (backgroundImageDiv);

            var backgroundColorDiv = Document.CreateElement (
                "div",
                "renderer-image-background-color",
                $"width: {image.Width}px !important;");
            imageContainer.AppendChild (backgroundColorDiv);

            // TODO: Reimplement with hover menu
            //if (RenderState.ParentState == null) {
            //    var sliderElem = (HtmlInputElement)Document.CreateElement ("input");
            //    sliderElem.Value = "0";
            //    sliderElem.Type = "range";
            //    container.AppendChild (sliderElem);

            //    Action<Event> slideHandler = evnt => {
            //        double val;
            //        if (!Double.TryParse (sliderElem.Value, out val))
            //            return;
            //        var opacity = (val / 100.0).ToString ();
            //        backgroundImageDiv.Style.SetProperty ("opacity", opacity);
            //        backgroundColorDiv.Style.SetProperty ("opacity", opacity);
            //    };

            //    sliderElem.AddEventListener ("input", slideHandler);
            //    // "input" is correct for live sliding events, but IE incorrectly implements using "change"
            //    sliderElem.AddEventListener ("change", slideHandler);
            //}

            container.AppendChild (imageContainer);
            return container;
        }
    }
}