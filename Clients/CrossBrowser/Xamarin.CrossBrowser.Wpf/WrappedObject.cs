//
// WrappedObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using mshtml;

namespace Xamarin.CrossBrowser
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.None)]
	public partial class WrappedObject
	{
		public ScriptContext Context { get; }
		internal readonly object ComObject;

		internal WrappedObject (ScriptContext context, object comObject)
		{
			if (context == null)
				throw new ArgumentNullException (nameof(context));

			if (comObject == null)
				throw new ArgumentNullException (nameof(comObject));

			Context = context;
			ComObject = comObject;
		}

		public override string ToString ()
		{
			try {
				// do not call Invoke here to prevent introducing a possible
				// cycle if anyone tries to add some CWL/ToString debugging.
				return (string)ComObject.GetType ().InvokeMember (
					"toString", BindingFlags.InvokeMethod, null, ComObject, null);
			} catch {
				return ComObject.ToString ();
			}
		}

		public virtual void SetProperty (string property, object value)
		{
			// NOTE: COM does not support defining new properties on an object via
			// InvokeMember (even with BindingFlags.PutDispProperty, etc.), so we
			// must go through the JS bridge which performs the property set in
			// JS code via a COM method invocation on the bridge. Sad.
			//
			// ComObject.GetType ().InvokeMember (property, BindingFlags.SetProperty,
			//	null, ComObject, new [] { ScriptContext.ToComObject (value) });

			Context.Bridge.SetProperty (this, property, value);
		}

		public virtual dynamic GetProperty (string property)
		{
			return Context.FromComObject (ComObject.GetType ().InvokeMember (
				property, BindingFlags.GetProperty, null, ComObject, null));
		}

		public virtual IEnumerable<dynamic> GetPropertyNames ()
		{
			dynamic propertyNames = Context.Bridge.GetPropertyNames (this);
			for (int i = 0; i < propertyNames.length; i++)
				yield return propertyNames [i];
		}

		public virtual bool HasProperty (string property) => Context.Bridge.HasProperty (this, property);

		dynamic ComInvoke (string method, object [] arguments)
		{
			return Context.FromComObject (ComObject.GetType ().InvokeMember (
				method, BindingFlags.InvokeMethod, null, ComObject, arguments));
		}

		public dynamic Invoke (string method, params object [] arguments)
		{
			return ComInvoke (method, Context.ToComObjectArray (arguments));
		}

		public dynamic Invoke (string method, IEnumerable<object> arguments)
		{
			return ComInvoke (method, Context.ToComObjectArray (arguments.ToArray ()));
		}

		public static bool operator == (WrappedObject a, WrappedObject b)
		{
			if (ReferenceEquals (a, b))
				return true;

			if ((object)a == null || (object)b == null)
				return false;

			return a.Equals (b);
		}

		public static bool operator != (WrappedObject a, WrappedObject b)
		{
			return !(a == b);
		}

		public override bool Equals (object obj)
		{
			var wrapped = obj as WrappedObject;
			return wrapped != null && ComObject.Equals (wrapped.ComObject);
		}

		public override int GetHashCode ()
		{
			return ComObject.GetHashCode (); 
		}

		internal T Wrap<T> (object value) where T : WrappedObject => Wrap<T> (Context, value);

		internal static T Wrap<T> (ScriptContext context, object value) where T : WrappedObject
		{
			return value == null ? null : (T)Inflate<T> (context, value);
		}

		static WrappedObject Inflate<T> (ScriptContext context, object nativeObject) where T : WrappedObject
		{
			if (nativeObject is IHTMLDocument2)
				return new HtmlDocument (context, (IHTMLDocument2)nativeObject);

			if (nativeObject is IHTMLStyleElement)
				return new HtmlStyleElement (context, (IHTMLStyleElement)nativeObject);

			if (nativeObject is IHTMLInputElement)
				return new HtmlInputElement(context, (IHTMLInputElement)nativeObject);

			if (nativeObject is IHTMLElement)
				return new HtmlElement (context, (IHTMLElement)nativeObject);

			if (nativeObject is IDOMKeyboardEvent)
				return new KeyboardEvent (context, (IDOMKeyboardEvent)nativeObject);

			if (nativeObject is IDOMUIEvent)
				return new UIEvent (context, (IDOMUIEvent)nativeObject);

			if (nativeObject is IDOMEvent)
				return new Event (context, (IDOMEvent)nativeObject);

			if (nativeObject is IHTMLDOMTextNode)
				return new Text (context, (IHTMLDOMTextNode)nativeObject);

			if (nativeObject is IHTMLDOMNode)
				return new Node (context, (IHTMLDOMNode)nativeObject);

			if (nativeObject is IHTMLSelection)
				return new Selection (context, (IHTMLSelection)nativeObject);

			if (nativeObject is IHTMLDOMRange)
				return new Range (context, (IHTMLDOMRange)nativeObject);

			if (nativeObject is IHTMLStyleSheetsCollection)
				return new StyleSheetList (context, (IHTMLStyleSheetsCollection)nativeObject);

			if (nativeObject is IHTMLStyleSheet)
				return new CssStyleSheet (context, (IHTMLStyleSheet)nativeObject);

			if (nativeObject is IHTMLCSSStyleDeclaration)
				return new CssStyleDeclaration (context, (IHTMLCSSStyleDeclaration)nativeObject);

			if (nativeObject is IHTMLCSSRule)
				return new CssRule (context, (IHTMLCSSRule)nativeObject);

			if (nativeObject is IHTMLRect)
				return new ClientRect (context, (IHTMLRect)nativeObject);

			return new WrappedObject (context, nativeObject);
		}

		protected static T Convert<T> (object o)
		{
			if (typeof(T) == typeof(string))
				return (T)(object)o?.ToString ();

			throw new ArgumentException ($"unable to convert to {typeof(T)}");
		}
	}
}