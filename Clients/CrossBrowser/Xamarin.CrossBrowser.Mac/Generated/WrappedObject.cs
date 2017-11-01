//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// WrappedObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class WrappedObject
	{
		internal readonly JSValue UnderlyingJSValue;

		internal WrappedObject (JSValue underlyingJSValue)
		{
			if (underlyingJSValue == null)
				throw new ArgumentNullException ("underlyingJSValue");
			UnderlyingJSValue = underlyingJSValue;
		}

		public override string ToString ()
		{
			return UnderlyingJSValue.Invoke ("toString").ToNullableString ();
		}
	}
}