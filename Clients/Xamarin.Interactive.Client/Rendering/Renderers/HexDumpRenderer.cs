//
// HexDumpRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof (byte[]))]
	[Renderer (typeof (Image))]
	sealed class HexDumpRenderer : HtmlRendererBase
	{
		static readonly RendererRepresentation representation = new RendererRepresentation ("Hex Dump");

		public override string CssClass => "renderer-hex-dump";
		public override bool CanExpand => true;

		byte [] data;

		protected override void HandleBind ()
		{
			data = RenderState.Source as byte [];
			if (data != null)
				return;

			data = (RenderState.Source as Image)?.Data;
		}

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return representation;
		}

		HtmlElement offsetColElem;
		HtmlElement hexColElem;
		HtmlElement asciiColElem;

		protected override void HandleRender (RenderTarget target)
		{
			if (data == null)
				return;

			target.InlineTarget.AppendChild (CreateHeaderElement (
				RenderState.Source, $"{data.Length:N0} bytes"));

			// FIXME: use flexbox instead of table?

			var tableElem = Document.CreateElement ("table");
			target.ExpandedTarget.AppendChild (tableElem);

			var rowElem = Document.CreateElement ("tr");
			tableElem.AppendChild (rowElem);

			offsetColElem = Document.CreateElement ("td");
			hexColElem = Document.CreateElement ("td");
			asciiColElem = Document.CreateElement ("td");
			rowElem.AppendChild (offsetColElem);
			rowElem.AppendChild (hexColElem);
			rowElem.AppendChild (asciiColElem);

			for (int i = 0; i < data.Length; i += 16) {
				RenderRow (i);

				// FIXME: load data as we scroll, reuse rows for rendering;
				// this is arbitrarily capped for now for performance reasons
				if (i > 1000)
					break;
			}
		}

		void RenderRow (int offset)
		{
			offsetColElem.AppendChild (Document.CreateElement (
				"div",
				innerText: $"{offset:x8}"));

			var hexRowElem = Document.CreateElement ("div");
			hexColElem.AppendChild (hexRowElem);

			var asciiRowElem = Document.CreateElement ("div");
			asciiColElem.AppendChild (asciiRowElem);

			for (int i = offset, n = offset + Math.Min (data.Length - offset, 16); i < n; i++) {

				hexRowElem.AppendChild (Document.CreateElement (
					"span",
					innerText: $"{data [i]:x2}"));

				var c = (char)data [i];
				string charClass = null;
				if (!Char.IsLetterOrDigit (c) && !Char.IsPunctuation (c) && c != ' ') {
					c = '.';
					charClass = "non-char";
				}

				asciiRowElem.AppendChild (Document.CreateElement (
					"span",
					@class: charClass,
					innerText: c.ToString ()));
			}
		}
	}
}