//
// VerbatimHtmlRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof (VerbatimHtml))]
	sealed class VerbatimHtmlRenderer : HtmlRendererBase
	{
		public override string CssClass => "renderer-verbatim-html";

		VerbatimHtml html;
		dynamic iframeElem;

		protected override void HandleBind ()=> html = (VerbatimHtml)RenderState.Source;

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return new RendererRepresentation (
				"HTML", options: RendererRepresentationOptions.ForceExpand);
		}

		/// <summary>
		/// Because an iframe is a separate document, we need to build it up such that it has
		/// the same rendering (IE Edge, HTML5) and encoding as our REPL shell document. We also
		/// need to copy parts of the computed sytlesheet as applied so far as the submission
		/// element. This is all because 'iframe seamless' is not supported anywhere :(
		/// </summary>
		StringBuilder GenerateHtmlWrapper (HtmlElement targetElem, string userHtml)
		{
			var builder = new StringBuilder ()
				.AppendLine ("<!DOCTYPE html>")
				.Append ("<html><head>")
				.Append ("<meta charset=\"utf-8\" />")
				.Append ("<meta http-equiv=\"content-type\" content=\"text/html; charset=utf-8\" />")
				.Append ("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />")
				// FIXME: we should probably supply a good default stylesheet here in addition
				// to translating some base properties from the shell document (e.g. things that
				// may get set dynamically).
				.Append ("<style type=\"text/css\">html, body {")
				.Append ("margin:0;")
				.Append ("padding:0;");

			var window = Document.Context.GlobalObject.window;
			// this will be <section class='submission'>:
			var style = window.getComputedStyle (targetElem.ParentElement);

			for (int i = 0; i < (int)style.length; i++) {
				var name = (string)style.item (i);
				foreach (var allowedPrefix in new [] { "text", "font", "color", "background" }) {
					if (name.StartsWith (allowedPrefix, StringComparison.Ordinal)) {
						var value = (string)style.getPropertyValue (name);
						if (!String.IsNullOrEmpty (value))
							builder.Append (name).Append (':').Append (value).Append (';');
						break;
					}
				}
			}

			return builder
				.Append ("</style></head><body>")
				.Append (userHtml)
				.Append ("</body></html>");
		}

		protected override void HandleRender (RenderTarget target)
		{
			iframeElem = Document.CreateElement ("iframe");
			target.ExpandedTarget.AppendChild (iframeElem);

			var htmlString = GenerateHtmlWrapper (target.ExpandedTarget, html.ToString ()).ToString ();

			iframeElem.onload = (ScriptAction)((s, a) =>
				iframeElem.style.height =
					iframeElem.contentWindow.document.documentElement.scrollHeight + "px");

			if (iframeElem.hasAttribute ("srcdoc")) {
				iframeElem.sandbox = "sandbox";
				iframeElem.srcdoc = htmlString;
				return;
			}

			// Set an arbitrary attribute on the element to our content (let's reuse srcdoc!)
			iframeElem.SetAttribute ("srcdoc", htmlString);

			// And now this terrible hack: browsers will implicitly set a document's contents
			// to a JS object's string representation... and the Browser happily executes via
			// javascript: scheme
			iframeElem.src = "javascript:window.frameElement.getAttribute('srcdoc')";
		}
	}
}