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
using mshtml;

namespace Xamarin.CrossBrowser
{
	public partial class ClientRect : WrappedObject
	{
		internal ClientRect (ScriptContext context, IHTMLRect comObject) : base (context, (Object)comObject)
		{
		}

		public double Left {
			get {
				return ((IHTMLRect)ComObject).left;
			}
		}

		public double Top {
			get {
				return ((IHTMLRect)ComObject).top;
			}
		}

		public double Right {
			get {
				return ((IHTMLRect)ComObject).right;
			}
		}

		public double Bottom {
			get {
				return ((IHTMLRect)ComObject).bottom;
			}
		}

		public double Width {
			get {
				return ((IHTMLRect2)ComObject).width;
			}
		}

		public double Height {
			get {
				return ((IHTMLRect2)ComObject).height;
			}
		}
	}
}