//
// WrappedObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class WrappedObject
	{
		public ScriptContext Context {
			get { return ScriptContext.Wrap (UnderlyingJSValue.Context); }
		}

		public void Inspect ()
		{
			foreach (var propertyName in GetPropertyNames ())
				Console.WriteLine ("{0} = {1}", propertyName, GetProperty (propertyName));
		}

		public string [] GetPropertyNames ()
		{
			return UnderlyingJSValue.GetPropertyNames ();
		}

		public object GetValueAt (int index)
		{
			return Context.FromJSValue (UnderlyingJSValue.GetValueAt ((nuint)index));
		}

		public object GetProperty (string property)
		{
			return Context.FromJSValue (UnderlyingJSValue.GetProperty (property));
		}

		public void SetProperty (string property, object value)
		{
			UnderlyingJSValue.SetProperty (Context.ToJSValue (value), property);
		}

		public bool HasProperty (string property)
		{
			return UnderlyingJSValue.HasProperty (property);
		}

		public object Invoke (string method, params object[] arguments)
		{
			return Invoke (method, (IEnumerable<object>)arguments);
		}

		public object Invoke (string method, IEnumerable<object> arguments)
		{
			var result = UnderlyingJSValue.Invoke (
				method,
				arguments.Select (Context.ToJSValue).ToArray ()
			);

			JSException.ThrowIfSet (Context.UnderlyingJSContext);

			return Context.FromJSValue (result);
		}

		public object Call (params object [] arguments)
		{
			return Call ((IEnumerable<object>)arguments);
		}

		public object Call (IEnumerable<object> arguments)
		{
			var result = UnderlyingJSValue.Call (
				arguments.Select (Context.ToJSValue).ToArray ()
			);

			JSException.ThrowIfSet (Context.UnderlyingJSContext);

			return Context.FromJSValue (result);
		}

		public object CallAsConstructor (params object [] arguments)
		{
			return CallAsConstructor ((IEnumerable<object>)arguments);
		}

		public object CallAsConstructor (IEnumerable<object> arguments)
		{
			var result = UnderlyingJSValue.CallAsConstructor (
				arguments.Select (Context.ToJSValue).ToArray (),
				Context.UnderlyingJSContext
			);

			JSException.ThrowIfSet (Context.UnderlyingJSContext);

			return Context.FromJSValue (result);
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
			return wrapped != null && UnderlyingJSValue.Equals (wrapped.UnderlyingJSValue);
		}

		public override int GetHashCode ()
		{
			return UnderlyingJSValue.GetHashCode ();
		}

		public static explicit operator bool (WrappedObject o)
			=> o.UnderlyingJSValue.ToBool ();

		static readonly Dictionary<string, Type> typeMap = new Dictionary<string, Type> ();

		public static T Wrap<T> (JSValue value) where T : WrappedObject
		{
			return value == null ? null : (T)Inflate<T> (value);
		}

		static WrappedObject Inflate<T> (JSValue value, Type wrappedType) where T : WrappedObject
		{
			return (T)Activator.CreateInstance (
				wrappedType,
				BindingFlags.NonPublic |
				BindingFlags.Public |
				BindingFlags.CreateInstance |
				BindingFlags.Instance,
				null,
				new object [] { value },
				null
			);
		}

		static WrappedObject Inflate<T> (JSValue value) where T : WrappedObject
		{
			if (value == null || value.IsNull || value.IsUndefined)
				return null;

			if (typeof(T) == typeof(WrappedObject))
				return new WrappedObject (value);
			if (typeof(T) == typeof(Range))
				return new Range (value);
			else if (typeof(T) == typeof(Selection))
				return new Selection (value);

			var valueStr = value.ToString ().ToLowerInvariant ();
			Type wrappedType;
			if (!typeMap.TryGetValue (valueStr, out wrappedType)) {
				if (valueStr == null ||
					!valueStr.StartsWith ("[object ", StringComparison.Ordinal) ||
					!valueStr.EndsWith ("]", StringComparison.Ordinal)) {
					// element.toString doesn't return [object HTMLWhateverElement] for some element types,
					// so if an object passes these tests, it's probably an HTML element anyway...
					// for whatever reason this always returns [object Window], which should be a sure
					// way to get the real type of an object (and works in the inspector console):
					//    value.Context.EvaluateScript ("Object.prototype.toString").Call (value)
					// sigh.
					if (value.HasProperty ("nodeType") && value.HasProperty ("nodeName")) {
						switch (value.GetProperty ("nodeType").ToInt32 ()) {
						case 1: // ELEMENT_NODE
							return value.HasProperty ("isContentEditable")
								? Inflate<T> (value, typeof(HtmlElement))
								: Inflate<T> (value, typeof(Element));
						}
					}

					return new WrappedObject (value);
				}

				var typeName = valueStr.Substring (8, valueStr.Length - 9);
				wrappedType = typeof(WrappedObject).Assembly.GetType (
					typeof(WrappedObject).Namespace + "." + typeName,
					false, true);

				if (wrappedType == null) {
					if (typeName.EndsWith ("element", StringComparison.Ordinal))
						wrappedType = typeName.StartsWith ("html", StringComparison.Ordinal)
							? typeof(HtmlElement)
							: typeof(Element);
					else if (typeName.EndsWith ("event", StringComparison.Ordinal))
						wrappedType = typeof(Event);
				}

				if (wrappedType == null)
					return new WrappedObject (value);

				typeMap.Add (valueStr, wrappedType);
			}

			return Inflate<T> (value, wrappedType);
		}
	}
}