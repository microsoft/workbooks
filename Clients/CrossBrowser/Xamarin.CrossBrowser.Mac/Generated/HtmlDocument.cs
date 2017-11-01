//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// HtmlDocument.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class HtmlDocument : Document
	{
		internal HtmlDocument (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public HtmlElement Head {
			get {
				return Wrap<HtmlElement> (UnderlyingJSValue.GetProperty ("head"));
			}
		}

		public HtmlElement Body {
			get {
				return Wrap<HtmlElement> (UnderlyingJSValue.GetProperty ("body"));
			}
		}

		public new HtmlElement CreateElement (string name)
		{
			return Wrap<HtmlElement> (UnderlyingJSValue.Invoke ("createElement", JSValue.From (name, UnderlyingJSValue.Context)));
		}

		public new HtmlElement GetElementById (string id)
		{
			return Wrap<HtmlElement> (UnderlyingJSValue.Invoke ("getElementById", JSValue.From (id, UnderlyingJSValue.Context)));
		}
	}
}