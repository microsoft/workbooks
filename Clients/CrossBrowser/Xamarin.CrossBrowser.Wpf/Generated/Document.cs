//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// Document.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
	public partial class Document : Node
	{
		internal Document (ScriptContext context, IHTMLDocument2 comObject) : base (context, (IHTMLDOMNode)comObject)
		{
		}

		public Element DocumentElement {
			get {
				return Wrap<Element> (((IHTMLDocument3)ComObject).documentElement);
			}
		}

		public Selection GetSelection ()
		{
			return Wrap<Selection> (((IHTMLDocument7)ComObject).getSelection ());
		}

		public Element CreateElement (string name)
		{
			return Wrap<Element> (((IHTMLDocument2)ComObject).createElement (name));
		}

		public Node CreateTextNode (string text)
		{
			return Wrap<Node> (((IHTMLDocument3)ComObject).createTextNode (text));
		}

		public Range CreateRange ()
		{
			return Wrap<Range> (((IDocumentRange)ComObject).createRange ());
		}

		public Element GetElementById (string id)
		{
			return Wrap<Element> (((IHTMLDocument3)ComObject).getElementById (id));
		}

		public StyleSheetList StyleSheets {
			get {
				return Wrap<StyleSheetList> (((IHTMLDocument2)ComObject).styleSheets);
			}
		}
	}
}