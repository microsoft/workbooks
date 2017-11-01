//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// Event.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class Event : WrappedObject
	{
		internal Event (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public bool Bubbles {
			get {
				return UnderlyingJSValue.GetProperty ("bubbles").ToBool ();
			}
		}

		public bool Cancelable {
			get {
				return UnderlyingJSValue.GetProperty ("cancelable").ToBool ();
			}
		}

		public EventTarget CurrentTarget {
			get {
				return Wrap<EventTarget> (UnderlyingJSValue.GetProperty ("currentTarget"));
			}
		}

		public bool DefaultPrevented {
			get {
				return UnderlyingJSValue.GetProperty ("defaultPrevented").ToBool ();
			}
		}

		public EventPhase EventPhase {
			get {
				return (EventPhase)UnderlyingJSValue.GetProperty ("eventPhase").ToUInt32 ();
			}
		}

		public EventTarget Target {
			get {
				return Wrap<EventTarget> (UnderlyingJSValue.GetProperty ("target"));
			}
		}

		public string Type {
			get {
				return UnderlyingJSValue.GetProperty ("type").ToNullableString ();
			}
		}

		public bool IsTrusted {
			get {
				return UnderlyingJSValue.GetProperty ("isTrusted").ToBool ();
			}
		}

		public void PreventDefault ()
		{
			UnderlyingJSValue.Invoke ("preventDefault");
		}

		public void StopImmediatePropagation ()
		{
			UnderlyingJSValue.Invoke ("stopImmediatePropagation");
		}

		public void StopPropagation ()
		{
			UnderlyingJSValue.Invoke ("stopPropagation");
		}
	}
}