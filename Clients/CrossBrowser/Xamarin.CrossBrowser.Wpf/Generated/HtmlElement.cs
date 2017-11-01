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
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class HtmlElement : Element
    {
        internal HtmlElement (ScriptContext context, IHTMLElement comObject) : base (context, comObject)
        {
        }

        public string ContentEditable {
            get {
                return ((IHTMLElement3)ComObject).contentEditable;
            }
            set {
                ((IHTMLElement3)ComObject).contentEditable = value;
            }
        }

        public bool IsContentEditable {
            get {
                return ((IHTMLElement3)ComObject).isContentEditable;
            }
        }

        public CssStyleDeclaration Style {
            get {
                return Wrap<CssStyleDeclaration> (((IHTMLElement)ComObject).style);
            }
        }

        public void Focus ()
        {
            ((IHTMLElement2)ComObject).focus ();
        }

        public void Blur ()
        {
            ((IHTMLElement2)ComObject).blur ();
        }

        public void Click ()
        {
            ((IHTMLElement)ComObject).click ();
        }

        public int ScrollTop {
            get {
                return ((IHTMLElement2)ComObject).scrollTop;
            }
            set {
                ((IHTMLElement2)ComObject).scrollTop = value;
            }
        }

        public int ScrollLeft {
            get {
                return ((IHTMLElement2)ComObject).scrollLeft;
            }
            set {
                ((IHTMLElement2)ComObject).scrollLeft = value;
            }
        }

        public int ScrollWidth {
            get {
                return ((IHTMLElement2)ComObject).scrollWidth;
            }
        }

        public int ScrollHeight {
            get {
                return ((IHTMLElement2)ComObject).scrollHeight;
            }
        }

        public double OffsetTop {
            get {
                return ((IHTMLElement)ComObject).offsetTop;
            }
        }

        public double OffsetLeft {
            get {
                return ((IHTMLElement)ComObject).offsetLeft;
            }
        }

        public double OffsetHeight {
            get {
                return ((IHTMLElement)ComObject).offsetHeight;
            }
        }

        public double OffsetWidth {
            get {
                return ((IHTMLElement)ComObject).offsetWidth;
            }
        }

        public HtmlElement OffsetParent {
            get {
                return Wrap<HtmlElement> (((IHTMLElement)ComObject).offsetParent);
            }
        }
    }
}