//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// CssRule.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class CssRule : WrappedObject
    {
        internal CssRule (ScriptContext context, IHTMLCSSRule comObject) : base (context, (Object)comObject)
        {
        }

        public string CssText {
            get {
                return ((IHTMLCSSRule)ComObject).cssText;
            }
        }

        public CssRule ParentRule {
            get {
                return Wrap<CssRule> (((IHTMLCSSRule)ComObject).parentRule);
            }
        }

        public CssStyleSheet ParentStyleSheet {
            get {
                return Wrap<CssStyleSheet> (((IHTMLCSSRule)ComObject).parentStyleSheet);
            }
        }

        public CssRuleType Type {
            get {
                return (CssRuleType)((IHTMLCSSRule)ComObject).type;
            }
        }
    }
}