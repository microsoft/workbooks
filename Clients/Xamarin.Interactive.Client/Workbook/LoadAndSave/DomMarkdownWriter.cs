//
// DomMarkdownWriter.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.IO;

using Xamarin.CrossBrowser;

namespace Xamarin.Interactive.Workbook.LoadAndSave
{
	sealed class DomMarkdownWriter
	{
		public void VisitHtmlElementChildren (HtmlElement elem, TextWriter writer)
		{
			var child = elem.FirstChild;
			while (child != null) {
				var htmlElemChild = child as HtmlElement;
				if (htmlElemChild != null)
					VisitHtmlElement (htmlElemChild, writer);
				else
					writer.Write (child.NodeValue);
				child = child.NextSibling;
			}
		}

		public void VisitHtmlElement (HtmlElement elem, TextWriter writer)
		{
			var inlineTag = String.Empty;

			switch (elem.TagName) {
			case "H1":
				writer.Write ("# ");
				break;
			case "H2":
				writer.Write ("## ");
				break;
			case "H3":
				writer.Write ("### ");
				break;
			case "H4":
				writer.Write ("#### ");
				break;
			case "H5":
				writer.Write ("##### ");
				break;
			case "H6":
				writer.Write ("###### ");
				break;
			case "B":
			case "STRONG":
				inlineTag = "**";
				break;
			case "I":
			case "EM":
				inlineTag = "_";
				break;
			case "BLOCKQUOTE":
				writer.Write ("> ");
				break;
			}

			writer.Write (inlineTag);

			VisitHtmlElementChildren (elem, writer);

			writer.Write (inlineTag);

			if (elem.TagName == "P") {
				writer.WriteLine ();
				writer.WriteLine ();
			}
		}
	}
}