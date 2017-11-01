//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using JavaScriptCore;
using ObjCRuntime;

namespace Xamarin.CrossBrowser
{
    static class JSValueExtensions
    {
        public static string ToNullableString (this JSValue value)
        {
            if (value.IsNull || value.IsUndefined || !value.IsString)
                return null;
            
            return value.ToString ();
        }

        // FIXME: ideally we'd set the function pointer for the managed delegate
        // on the JS function object itself with JSObjectSetPrivate, but unfortunately
        // JSObjectMakeFunctionWithCallback does not allocate a function object that
        // has space for user data. For that we'd need to set up the whole class
        // definition ourselves and set the callAsFunction function, and then call
        // JSObjectMake. I just don't feel like binding all of that.
        static readonly Dictionary<IntPtr, ScriptFunc> functionDelegates
            = new Dictionary<IntPtr, ScriptFunc> ();

        unsafe static readonly JSObjectCallAsFunctionCallback trampoline
            = new JSObjectCallAsFunctionCallback (Trampoline);

        [MonoPInvokeCallback (typeof (JSObjectCallAsFunctionCallback))]
        unsafe static IntPtr Trampoline (IntPtr contextPtr, IntPtr function, IntPtr thisObject,
            IntPtr argumentCount, IntPtr *arguments, IntPtr exception)
        {
            var globalContextPtr = JSContextGetGlobalContext (contextPtr);
            var context = JSContext.FromJSGlobalContextRef (globalContextPtr);

            var args = new WrappedObject [(int)argumentCount];
            for (int i = 0; i < args.Length; i++)
                args [i] = new WrappedObject (JSValue.FromJSJSValueRef (arguments [i], context));

            var retval = functionDelegates [function] (
                new WrappedObject (JSValue.FromJSJSValueRef (thisObject, context)),
                args);

            if (retval == null)
                return JSValue.Null (context).JSValueRefPtr;

            if (retval is bool)
                return JSValue.From ((bool)retval, context).JSValueRefPtr;

            if (retval is int)
                return JSValue.From ((int)retval, context).JSValueRefPtr;

            if (retval is uint)
                return JSValue.From ((uint)retval, context).JSValueRefPtr;

            if (retval is double)
                return JSValue.From ((double)retval, context).JSValueRefPtr;

            if (retval is string)
                return JSValue.From ((string)retval, context).JSValueRefPtr;

            return retval.UnderlyingJSValue.JSValueRefPtr;
        }

        public static JSValue ToJSValue (this Action scriptAction, JSContext context)
        {
            return ToJSValue (new ScriptFunc ((o, args) => {
                scriptAction ();
                return new WrappedObject (JSValue.Undefined (context));
            }), context);
        }

        public static JSValue ToJSValue (this ScriptAction scriptAction, JSContext context)
        {
            return ToJSValue (new ScriptFunc ((o, args) => {
                scriptAction (o, args);
                return new WrappedObject (JSValue.Undefined (context));
            }), context);
        }

        public static JSValue ToJSValue (this ScriptFunc scriptFunc, JSContext context)
        {
            var functionObj = JSObjectMakeFunctionWithCallback (
                context.JSGlobalContextRefPtr,
                IntPtr.Zero,
                trampoline);

            functionDelegates [functionObj] = scriptFunc;

            return JSValue.FromJSJSValueRef (functionObj, context);
        }

        public static string [] GetPropertyNames (this JSValue value)
        {
            var arrayPtr = JSObjectCopyPropertyNames (
                value.Context.JSGlobalContextRefPtr,
                value.JSValueRefPtr);

            var count = (int)JSPropertyNameArrayGetCount (arrayPtr);
            var array = new string [count];

            for (int i = 0; i < count; i++) {
                var stringPtr = JSPropertyNameArrayGetNameAtIndex (arrayPtr, new IntPtr (i));
                array [i] = JSStringToString (stringPtr);
            }

            JSPropertyNameArrayRelease (arrayPtr);

            return array;
        }

        [UnmanagedFunctionPointerAttribute (CallingConvention.Cdecl)]
        unsafe delegate IntPtr JSObjectCallAsFunctionCallback (
            IntPtr ctx, IntPtr function, IntPtr thisObject,
            IntPtr argumentCount, IntPtr *arguments,
            IntPtr exception);

        [DllImport (Constants.JavaScriptCoreLibrary)]
        static extern IntPtr JSObjectMakeFunctionWithCallback (IntPtr ctx, IntPtr name,
            JSObjectCallAsFunctionCallback callAsFunction);

        [DllImport (Constants.JavaScriptCoreLibrary)]
        static extern IntPtr JSContextGetGlobalContext (IntPtr ctx);

        [DllImport (Constants.JavaScriptCoreLibrary)]
        static extern IntPtr JSObjectCopyPropertyNames (IntPtr ctx, IntPtr @object);

        [DllImport (Constants.JavaScriptCoreLibrary)]
        static extern void JSPropertyNameArrayRelease (IntPtr array);

        [DllImport (Constants.JavaScriptCoreLibrary)]
        static extern IntPtr JSPropertyNameArrayGetCount (IntPtr array);

        [DllImport (Constants.JavaScriptCoreLibrary)]
        static extern IntPtr JSPropertyNameArrayGetNameAtIndex (IntPtr array, IntPtr index);

        [DllImport (Constants.JavaScriptCoreLibrary)]
        static extern IntPtr JSStringGetMaximumUTF8CStringSize (IntPtr @string);

        [DllImport (Constants.JavaScriptCoreLibrary)]
        unsafe static extern IntPtr JSStringGetUTF8CString (IntPtr @string, byte* buffer, IntPtr bufferSize);

        unsafe static string JSStringToString (IntPtr @string)
        {
            if (@string == IntPtr.Zero)
                return null;

            var bufferSize = (int)JSStringGetMaximumUTF8CStringSize (@string);
            var buffer = new byte [bufferSize];
            fixed (byte *bufferPtr = &buffer[0]) {
                var length = JSStringGetUTF8CString (@string, bufferPtr, new IntPtr (bufferSize));
                return Encoding.UTF8.GetString (buffer, 0, (int)length - 1);
            }
        }

        [DllImport (Constants.JavaScriptCoreLibrary)]
        static extern IntPtr JSObjectCallAsConstructor (IntPtr ctx, IntPtr obj,
            IntPtr argumentCount, IntPtr [] arguments, out IntPtr exception);

        public static JSValue CallAsConstructor (this JSValue obj, JSValue[] arguments, JSContext context)
        {
            if (obj == null)
                throw new ArgumentNullException (nameof(obj));

            if (context == null)
                throw new ArgumentNullException (nameof(obj));

            IntPtr[] argumentPtrs = null;
            var argumentCount = IntPtr.Zero;
            if (arguments != null && arguments.Length > 0) {
                argumentCount = new IntPtr (arguments.Length);
                argumentPtrs = new IntPtr [arguments.Length];
                for (int i = 0; i < arguments.Length; i++)
                    argumentPtrs [i] = arguments [i].JSValueRefPtr;
            }

            IntPtr exceptionPtr;
            var resultPtr = JSObjectCallAsConstructor (
                context.JSGlobalContextRefPtr,
                obj.JSValueRefPtr,
                argumentCount,
                argumentPtrs,
                out exceptionPtr);

            if (exceptionPtr != IntPtr.Zero)
                throw new JSException (exceptionPtr, context);

            return JSValue.FromJSJSValueRef (resultPtr, context);
        }
    }
}