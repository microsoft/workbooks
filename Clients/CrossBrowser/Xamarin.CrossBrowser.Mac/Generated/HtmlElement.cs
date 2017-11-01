//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// HtmlElement.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public partial class HtmlElement : Element
    {
        internal HtmlElement (JSValue underlyingJSValue) : base (underlyingJSValue)
        {
        }

        public string ContentEditable {
            get {
                return UnderlyingJSValue.GetProperty ("contentEditable").ToNullableString ();
            }
            set {
                UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "contentEditable");
            }
        }

        public bool IsContentEditable {
            get {
                return UnderlyingJSValue.GetProperty ("isContentEditable").ToBool ();
            }
        }

        public CssStyleDeclaration Style {
            get {
                return Wrap<CssStyleDeclaration> (UnderlyingJSValue.GetProperty ("style"));
            }
        }

        public void Focus ()
        {
            UnderlyingJSValue.Invoke ("focus");
        }

        public void Blur ()
        {
            UnderlyingJSValue.Invoke ("blur");
        }

        public void Click ()
        {
            UnderlyingJSValue.Invoke ("click");
        }

        public int ScrollTop {
            get {
                return UnderlyingJSValue.GetProperty ("scrollTop").ToInt32 ();
            }
            set {
                UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "scrollTop");
            }
        }

        public int ScrollLeft {
            get {
                return UnderlyingJSValue.GetProperty ("scrollLeft").ToInt32 ();
            }
            set {
                UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "scrollLeft");
            }
        }

        public int ScrollWidth {
            get {
                return UnderlyingJSValue.GetProperty ("scrollWidth").ToInt32 ();
            }
        }

        public int ScrollHeight {
            get {
                return UnderlyingJSValue.GetProperty ("scrollHeight").ToInt32 ();
            }
        }

        public double OffsetTop {
            get {
                return UnderlyingJSValue.GetProperty ("offsetTop").ToDouble ();
            }
        }

        public double OffsetLeft {
            get {
                return UnderlyingJSValue.GetProperty ("offsetLeft").ToDouble ();
            }
        }

        public double OffsetHeight {
            get {
                return UnderlyingJSValue.GetProperty ("offsetHeight").ToDouble ();
            }
        }

        public double OffsetWidth {
            get {
                return UnderlyingJSValue.GetProperty ("offsetWidth").ToDouble ();
            }
        }

        public HtmlElement OffsetParent {
            get {
                return Wrap<HtmlElement> (UnderlyingJSValue.GetProperty ("offsetParent"));
            }
        }
    }
}