//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// StyleSheet.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class StyleSheet : WrappedObject
	{
		internal StyleSheet (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public string Href {
			get {
				return UnderlyingJSValue.GetProperty ("href").ToNullableString ();
			}
		}

		public string Id {
			get {
				return UnderlyingJSValue.GetProperty ("id").ToNullableString ();
			}
		}

		public string Title {
			get {
				return UnderlyingJSValue.GetProperty ("title").ToNullableString ();
			}
		}

		public string Type {
			get {
				return UnderlyingJSValue.GetProperty ("type").ToNullableString ();
			}
		}

		public Node OwnerNode {
			get {
				return Wrap<Node> (UnderlyingJSValue.GetProperty ("ownerNode"));
			}
		}
	}
}