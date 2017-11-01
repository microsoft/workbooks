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
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public partial class HtmlInputElement : HtmlElement
    {
        internal HtmlInputElement (JSValue underlyingJSValue) : base (underlyingJSValue)
        {
        }

        public string Type {
            get {
                return UnderlyingJSValue.GetProperty ("type").ToNullableString ();
            }
            set {
                UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "type");
            }
        }

        public string Value {
            get {
                return UnderlyingJSValue.GetProperty ("value").ToNullableString ();
            }
            set {
                UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "value");
            }
        }
    }
}