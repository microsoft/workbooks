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
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class CssRule : WrappedObject
	{
		internal CssRule (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public string CssText {
			get {
				return UnderlyingJSValue.GetProperty ("cssText").ToNullableString ();
			}
		}

		public CssRule ParentRule {
			get {
				return Wrap<CssRule> (UnderlyingJSValue.GetProperty ("parentRule"));
			}
		}

		public CssStyleSheet ParentStyleSheet {
			get {
				return Wrap<CssStyleSheet> (UnderlyingJSValue.GetProperty ("parentStyleSheet"));
			}
		}

		public CssRuleType Type {
			get {
				return (CssRuleType)UnderlyingJSValue.GetProperty ("type").ToUInt32 ();
			}
		}
	}
}