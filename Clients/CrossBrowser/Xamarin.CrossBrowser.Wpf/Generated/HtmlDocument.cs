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
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class HtmlDocument : Document
    {
        internal HtmlDocument (ScriptContext context, IHTMLDocument2 comObject) : base (context, comObject)
        {
        }

        public HtmlElement Head {
            get {
                return Wrap<HtmlElement> (((IHTMLDocument7)ComObject).head);
            }
        }

        public HtmlElement Body {
            get {
                return Wrap<HtmlElement> (((IHTMLDocument2)ComObject).body);
            }
        }

        public new HtmlElement DocumentElement {
            get {
                return Wrap<HtmlElement> (((IHTMLDocument3)ComObject).documentElement);
            }
        }

        public new HtmlElement CreateElement (string name)
        {
            return Wrap<HtmlElement> (((IHTMLDocument2)ComObject).createElement (name));
        }

        public new HtmlElement GetElementById (string id)
        {
            return Wrap<HtmlElement> (((IHTMLDocument3)ComObject).getElementById (id));
        }
    }
}