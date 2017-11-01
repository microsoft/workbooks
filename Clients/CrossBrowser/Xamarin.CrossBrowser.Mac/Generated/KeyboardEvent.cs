//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// KeyboardEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class KeyboardEvent : UIEvent
	{
		internal KeyboardEvent (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public bool AltKey {
			get {
				return UnderlyingJSValue.GetProperty ("altKey").ToBool ();
			}
		}

		public bool CtrlKey {
			get {
				return UnderlyingJSValue.GetProperty ("ctrlKey").ToBool ();
			}
		}

		public bool MetaKey {
			get {
				return UnderlyingJSValue.GetProperty ("metaKey").ToBool ();
			}
		}

		public bool ShiftKey {
			get {
				return UnderlyingJSValue.GetProperty ("shiftKey").ToBool ();
			}
		}

		public bool Repeat {
			get {
				return UnderlyingJSValue.GetProperty ("repeat").ToBool ();
			}
		}

		public int KeyCode {
			get {
				return UnderlyingJSValue.GetProperty ("keyCode").ToInt32 ();
			}
		}

		public int CharCode {
			get {
				return UnderlyingJSValue.GetProperty ("charCode").ToInt32 ();
			}
		}

		public string Key {
			get {
				return UnderlyingJSValue.GetProperty ("key").ToNullableString ();
			}
		}
	}
}