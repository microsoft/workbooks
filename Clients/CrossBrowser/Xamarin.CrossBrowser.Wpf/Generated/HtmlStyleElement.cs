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
using mshtml;

namespace Xamarin.CrossBrowser
{
	public partial class HtmlStyleElement : HtmlElement
	{
		internal HtmlStyleElement (ScriptContext context, IHTMLStyleElement comObject) : base (context, (IHTMLElement)comObject)
		{
		}

		public CssStyleSheet Sheet {
			get {
				return Wrap<CssStyleSheet> (((IHTMLStyleElement2)ComObject).sheet);
			}
		}

		public string Type {
			get {
				return ((IHTMLStyleElement)ComObject).type;
			}
			set {
				((IHTMLStyleElement)ComObject).type = value;
			}
		}

		public string Media {
			get {
				return ((IHTMLStyleElement)ComObject).media;
			}
			set {
				((IHTMLStyleElement)ComObject).media = value;
			}
		}

		public bool Disabled {
			get {
				return ((IHTMLStyleElement)ComObject).disabled;
			}
			set {
				((IHTMLStyleElement)ComObject).disabled = value;
			}
		}
	}
}