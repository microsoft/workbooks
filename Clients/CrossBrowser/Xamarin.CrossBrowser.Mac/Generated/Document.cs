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
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class Document : Node
	{
		internal Document (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public Element DocumentElement {
			get {
				return Wrap<Element> (UnderlyingJSValue.GetProperty ("documentElement"));
			}
		}

		public Selection GetSelection ()
		{
			return Wrap<Selection> (UnderlyingJSValue.Invoke ("getSelection"));
		}

		public Element CreateElement (string name)
		{
			return Wrap<Element> (UnderlyingJSValue.Invoke ("createElement", JSValue.From (name, UnderlyingJSValue.Context)));
		}

		public Node CreateTextNode (string text)
		{
			return Wrap<Node> (UnderlyingJSValue.Invoke ("createTextNode", JSValue.From (text, UnderlyingJSValue.Context)));
		}

		public Range CreateRange ()
		{
			return Wrap<Range> (UnderlyingJSValue.Invoke ("createRange"));
		}

		public Element GetElementById (string id)
		{
			return Wrap<Element> (UnderlyingJSValue.Invoke ("getElementById", JSValue.From (id, UnderlyingJSValue.Context)));
		}

		public StyleSheetList StyleSheets {
			get {
				return Wrap<StyleSheetList> (UnderlyingJSValue.GetProperty ("styleSheets"));
			}
		}
	}
}