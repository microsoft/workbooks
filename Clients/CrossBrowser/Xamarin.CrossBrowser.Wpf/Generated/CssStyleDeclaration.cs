//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// CssStyleDeclaration.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class CssStyleDeclaration : WrappedObject
    {
        internal CssStyleDeclaration (ScriptContext context, IHTMLCSSStyleDeclaration comObject) : base (context, (Object)comObject)
        {
        }

        public string CssText {
            get {
                return ((IHTMLCSSStyleDeclaration)ComObject).cssText;
            }
            set {
                ((IHTMLCSSStyleDeclaration)ComObject).cssText = value;
            }
        }

        public int Length {
            get {
                return ((IHTMLCSSStyleDeclaration)ComObject).length;
            }
        }

        public CssRule ParentRule {
            get {
                return Wrap<CssRule> (((IHTMLCSSStyleDeclaration)ComObject).parentRule);
            }
        }

        public string Item (int index)
        {
            return ((IHTMLCSSStyleDeclaration)ComObject).item (index);
        }

        public void SetProperty (string name, string value, string priority = "")
        {
            ((IHTMLCSSStyleDeclaration)ComObject).setProperty (name, value, priority);
        }

        public void RemoveProperty (string name)
        {
            ((IHTMLCSSStyleDeclaration)ComObject).removeProperty (name);
        }

        public string GetPropertyValue (string name)
        {
            return ((IHTMLCSSStyleDeclaration)ComObject).getPropertyValue (name);
        }

        public string GetPropertyPriority (string name)
        {
            return ((IHTMLCSSStyleDeclaration)ComObject).getPropertyPriority (name);
        }
    }
}