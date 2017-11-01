//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// ClientRect.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class ClientRect : WrappedObject
	{
		internal ClientRect (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public double Left {
			get {
				return UnderlyingJSValue.GetProperty ("left").ToDouble ();
			}
		}

		public double Top {
			get {
				return UnderlyingJSValue.GetProperty ("top").ToDouble ();
			}
		}

		public double Right {
			get {
				return UnderlyingJSValue.GetProperty ("right").ToDouble ();
			}
		}

		public double Bottom {
			get {
				return UnderlyingJSValue.GetProperty ("bottom").ToDouble ();
			}
		}

		public double Width {
			get {
				return UnderlyingJSValue.GetProperty ("width").ToDouble ();
			}
		}

		public double Height {
			get {
				return UnderlyingJSValue.GetProperty ("height").ToDouble ();
			}
		}
	}
}