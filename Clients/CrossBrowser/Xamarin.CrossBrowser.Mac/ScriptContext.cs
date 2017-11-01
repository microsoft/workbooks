//
// ScriptContext.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;

using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public sealed class ScriptContext
    {
        static readonly Dictionary<JSContext, ScriptContext> contexts
            = new Dictionary<JSContext, ScriptContext> ();

        internal static ScriptContext Wrap (JSContext underlyingContext)
        {
            if (underlyingContext == null)
                return null;

            ScriptContext context;
            if (!contexts.TryGetValue (underlyingContext, out context))
                contexts.Add (
                    underlyingContext,
                    context = new ScriptContext (underlyingContext));
            return context;
        }

        readonly JSContext underlyingContext;
        internal JSContext UnderlyingJSContext {
            get { return underlyingContext; }
        }

        ScriptContext (JSContext underlyingContext)
        {
            this.underlyingContext = underlyingContext;
        }

        public dynamic GlobalObject {
            get { return WrappedObject.Wrap<WrappedObject> (underlyingContext.GlobalObject); }
        }

        public dynamic CreateObject (Action<dynamic> populator = null)
        {
            var wrapped = new WrappedObject (JSValue.CreateObject (underlyingContext));
            populator?.Invoke (wrapped);
            return wrapped;
        }

        public dynamic CreateArray ()
        {
            return new WrappedObject (JSValue.CreateArray (underlyingContext));
        }

        internal object FromJSValue (JSValue value)
        {
            if (value.IsNull || value.IsUndefined)
                return null;

            if (value.IsBoolean)
                return value.ToBool ();

            if (value.IsNumber) {
                var n = value.ToDouble ();
                if (n % 1 != 0)
                    return n;

                var l = (long)n;
                if (l <= Int32.MaxValue)
                    return (int)l;

                return l;
            }

            if (value.IsString)
                return value.ToString ();

            return new WrappedObject (value);
        }

        internal JSValue ToJSValue (object o)
        {
            if (o == null)
                return JSValue.Null (underlyingContext);

            if (o is JSValue)
                return (JSValue)o;

            if (o is WrappedObject)
                return ((WrappedObject)o).UnderlyingJSValue;

            if (o is ScriptAction)
                return ((ScriptAction)o).ToJSValue (underlyingContext);

            if (o is ScriptFunc)
                return ((ScriptFunc)o).ToJSValue (underlyingContext);

            if (o is Action)
                return ((Action)o).ToJSValue (underlyingContext);

            switch (Convert.GetTypeCode (o)) {
            case TypeCode.Boolean:
                return JSValue.From ((bool)o, underlyingContext);
            case TypeCode.SByte:
                return JSValue.From ((int)(sbyte)o, underlyingContext);
            case TypeCode.Byte:
                return JSValue.From ((uint)(byte)o, underlyingContext);
            case TypeCode.Int16:
                return JSValue.From ((int)(short)o, underlyingContext);
            case TypeCode.UInt16:
                return JSValue.From ((uint)(ushort)o, underlyingContext);
            case TypeCode.Int32:
                return JSValue.From ((int)o, underlyingContext);
            case TypeCode.UInt32:
                return JSValue.From ((uint)o, underlyingContext);
            case TypeCode.Single:
                return JSValue.From ((double)(float)o, underlyingContext);
            case TypeCode.Double:
                return JSValue.From ((double)o, underlyingContext);
            case TypeCode.String:
                return JSValue.From ((string)o, underlyingContext);
            case TypeCode.Char:
                return JSValue.From (o.ToString (), underlyingContext);
            }

            throw new ArgumentException ("cannot convert type '" + o.GetType () + "' to JSValue");
        }
    }
}