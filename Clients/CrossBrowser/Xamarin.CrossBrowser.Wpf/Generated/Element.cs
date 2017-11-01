//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// Element.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class Element : Node
    {
        internal Element (ScriptContext context, IHTMLElement comObject) : base (context, (IHTMLDOMNode)comObject)
        {
        }

        public string TagName {
            get {
                return ((IHTMLElement)ComObject).tagName;
            }
        }

        public string ClassName {
            get {
                return ((IHTMLElement)ComObject).className;
            }
            set {
                ((IHTMLElement)ComObject).className = value;
            }
        }

        public string Id {
            get {
                return ((IHTMLElement)ComObject).id;
            }
            set {
                ((IHTMLElement)ComObject).id = value;
            }
        }

        public string InnerHTML {
            get {
                return ((IHTMLElement)ComObject).innerHTML;
            }
            set {
                ((IHTMLElement)ComObject).innerHTML = value;
            }
        }

        public string OuterHTML {
            get {
                return ((IHTMLElement)ComObject).outerHTML;
            }
            set {
                ((IHTMLElement)ComObject).outerHTML = value;
            }
        }

        public Element ParentElement {
            get {
                return Wrap<Element> (((IHTMLElement)ComObject).parentElement);
            }
        }

        public double ClientTop {
            get {
                return ((IHTMLElement2)ComObject).clientTop;
            }
        }

        public double ClientLeft {
            get {
                return ((IHTMLElement2)ComObject).clientLeft;
            }
        }

        public double ClientHeight {
            get {
                return ((IHTMLElement2)ComObject).clientHeight;
            }
        }

        public double ClientWidth {
            get {
                return ((IHTMLElement2)ComObject).clientWidth;
            }
        }

        public int ChildElementCount {
            get {
                return ((IElementTraversal)ComObject).childElementCount;
            }
        }

        public Element FirstElementChild {
            get {
                return Wrap<Element> (((IElementTraversal)ComObject).firstElementChild);
            }
        }

        public Element LastElementChild {
            get {
                return Wrap<Element> (((IElementTraversal)ComObject).lastElementChild);
            }
        }

        public Element NextElementSibling {
            get {
                return Wrap<Element> (((IElementTraversal)ComObject).nextElementSibling);
            }
        }

        public Element PreviousElementSibling {
            get {
                return Wrap<Element> (((IElementTraversal)ComObject).previousElementSibling);
            }
        }

        public bool Contains (Element other)
        {
            return ((IHTMLElement)ComObject).contains ((IHTMLElement)other.ComObject);
        }

        public void Normalize ()
        {
            ((IHTMLElement4)ComObject).normalize ();
        }

        public void SetAttribute (string name, string value)
        {
            ((IHTMLElement5)ComObject).setAttribute (name, value);
        }

        public bool HasAttribute (string name)
        {
            return ((IHTMLElement5)ComObject).hasAttribute (name);
        }

        public string GetAttribute (string name)
        {
            return Convert<string> (((IHTMLElement5)ComObject).getAttribute (name));
        }

        public void RemoveAttribute (string name)
        {
            ((IHTMLElement5)ComObject).removeAttribute (name);
        }

        public ClientRect GetBoundingClientRect ()
        {
            return Wrap<ClientRect> (((IHTMLElement2)ComObject).getBoundingClientRect ());
        }

        public void ScrollIntoView (bool alignToTop = true)
        {
            ((IHTMLElement)ComObject).scrollIntoView (alignToTop);
        }
    }
}