//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// HtmlInputElement.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class HtmlInputElement : HtmlElement
    {
        internal HtmlInputElement (ScriptContext context, IHTMLInputElement comObject) : base (context, (IHTMLElement)comObject)
        {
        }

        public string Type {
            get {
                return ((IHTMLInputElement)ComObject).type;
            }
            set {
                ((IHTMLInputElement)ComObject).type = value;
            }
        }

        public string Value {
            get {
                return ((IHTMLInputElement)ComObject).value;
            }
            set {
                ((IHTMLInputElement)ComObject).value = value;
            }
        }
    }
}