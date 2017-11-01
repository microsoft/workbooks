//
// JSException.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	class JSException : Exception
	{
		public JSValue Value { get; }

		public JSException (IntPtr exceptionPtr, IntPtr contextPtr)
			: this (exceptionPtr, JSContext.FromJSGlobalContextRef (contextPtr))
		{
		}

		public JSException (IntPtr exceptionPtr, JSContext context)
			: this (JSValue.FromJSJSValueRef (exceptionPtr, context))
		{
		}

		public JSException (JSValue exception) : base (exception.ToString ())
		{
			Value = exception;
		}

		public static void ThrowIfSet (JSContext context)
		{
			if (context.Exception != null)
				throw new JSException (context.Exception);
		}
	}
}