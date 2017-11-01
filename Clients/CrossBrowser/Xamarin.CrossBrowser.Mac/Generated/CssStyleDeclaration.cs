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
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class CssStyleDeclaration : WrappedObject
	{
		internal CssStyleDeclaration (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public string CssText {
			get {
				return UnderlyingJSValue.GetProperty ("cssText").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "cssText");
			}
		}

		public int Length {
			get {
				return UnderlyingJSValue.GetProperty ("length").ToInt32 ();
			}
		}

		public CssRule ParentRule {
			get {
				return Wrap<CssRule> (UnderlyingJSValue.GetProperty ("parentRule"));
			}
		}

		public string Item (int index)
		{
			return UnderlyingJSValue.Invoke ("item", JSValue.From (index, UnderlyingJSValue.Context)).ToNullableString ();
		}

		public void SetProperty (string name, string value, string priority = "")
		{
			UnderlyingJSValue.Invoke ("setProperty", JSValue.From (name, UnderlyingJSValue.Context), JSValue.From (value, UnderlyingJSValue.Context), JSValue.From (priority, UnderlyingJSValue.Context));
		}

		public void RemoveProperty (string name)
		{
			UnderlyingJSValue.Invoke ("removeProperty", JSValue.From (name, UnderlyingJSValue.Context));
		}

		public string GetPropertyValue (string name)
		{
			return UnderlyingJSValue.Invoke ("getPropertyValue", JSValue.From (name, UnderlyingJSValue.Context)).ToNullableString ();
		}

		public string GetPropertyPriority (string name)
		{
			return UnderlyingJSValue.Invoke ("getPropertyPriority", JSValue.From (name, UnderlyingJSValue.Context)).ToNullableString ();
		}
	}
}