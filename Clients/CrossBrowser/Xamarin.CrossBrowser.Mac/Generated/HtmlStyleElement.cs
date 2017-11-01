//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// HtmlStyleElement.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class HtmlStyleElement : HtmlElement
	{
		internal HtmlStyleElement (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public CssStyleSheet Sheet {
			get {
				return Wrap<CssStyleSheet> (UnderlyingJSValue.GetProperty ("sheet"));
			}
		}

		public string Type {
			get {
				return UnderlyingJSValue.GetProperty ("type").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "type");
			}
		}

		public string Media {
			get {
				return UnderlyingJSValue.GetProperty ("media").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "media");
			}
		}

		public bool Disabled {
			get {
				return UnderlyingJSValue.GetProperty ("disabled").ToBool ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "disabled");
			}
		}
	}
}