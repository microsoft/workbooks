//
// ScriptContext.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Controls;

using mshtml;

namespace Xamarin.CrossBrowser
{
    public sealed class ScriptContext
    {
        [ComVisible (true)]
        public class ObjectForScripting
        {
            readonly ScriptContext context;

            internal ObjectForScripting (ScriptContext context)
            {
                this.context = context;
            }

            public object __xcb_InvokeProxy (int functionId, object thisArg, object arguments) =>
                context.InvokeFunction (functionId, thisArg, arguments);
        }

        readonly WebBrowser webBrowser;

        internal ScriptBridge Bridge { get; private set; }
        public dynamic GlobalObject { get; private set; }

        public ScriptContext (WebBrowser webBrowser)
        {
            this.webBrowser = webBrowser;
            webBrowser.ObjectForScripting = new ObjectForScripting (this);
        }

        internal void Initialize ()
        {
            var document = (IHTMLDocument2)webBrowser.Document;
            document.parentWindow.execScript (ScriptBridge.BackingJS);

            Bridge = new ScriptBridge (this, webBrowser.InvokeScript ("__xcb_getBridge"));
            GlobalObject = FromComObject (document.parentWindow);
        }

        public dynamic CreateObject (Action<dynamic> populator = null)
        {
            var o = Bridge.CreateObject ();
            populator?.Invoke (o);
            return o;
        }

        public dynamic CreateArray () => Bridge.CreateArray ();

        #region COM Conversions

        internal object FromComObject (object o)
        {
            if (o == null)
                return null;

            if (o.GetType ().IsCOMObject)
                return new WrappedObject (this, o);

            return o;
        }

        internal object ToComObject (object o)
        {
            if (o is WrappedObject)
                return ((WrappedObject)o).ComObject;

            // NOTE: we have two paths for handling callbacks from JS to managed:
            //
            // - We proxy ScriptFunctions through Bridge.CreateFunction to wrap
            //   the delegate in a pure JS function
            // - ScriptFunction itself is a COM object implementing IReflect
            //   and can be passed directly
            //
            // The wrapped method is probably slower, but possibly more safe since
            // pure JS semantics are kept, but the COM method is probably much
            // faster, but there may be some unknown semantics that could cause
            // problems in the future. Both methods are kept around so we can
            // switch between them. ScriptFunction's internals are largely shared
            // between both methods. Simply returning a ScriptFunction in this
            // method enables the pure COM path.
            //
            // In both cases we register the function. For the wrapper method, we
            // need the registration to map from a registration ID to the managed
            // function. In both cases the registration ensures the function is
            // not GCed.
            // const bool UsePureJSFunctionWrapping = true;

            if (o is ScriptAction)
                o = new ScriptFunction (this, (ScriptAction)o);

            if (o is ScriptFunc)
                o = new ScriptFunction (this, (ScriptFunc)o);

            if (o is ScriptFunction) {
                var function = (ScriptFunction)o;
                var functionId = RegisterFunction (function);
                //if (UsePureJSFunctionWrapping)
                    return Bridge.CreateFunction (functionId);
                //return function;
            }

            return o;
        }

        internal object [] ToComObjectArray (object [] objects)
        {
            if (objects == null || objects.Length == 0)
                return null;
            var comObjects = new object [objects.Length];
            for (int i = 0; i < objects.Length; i++)
                comObjects [i] = ToComObject (objects [i]);
            return comObjects;
        }

        #endregion

        #region Function Proxying

        int lastFunctionId = 0;
        readonly Dictionary<int, ScriptFunction> functions = new Dictionary<int, ScriptFunction> ();

        int RegisterFunction (ScriptFunction function)
        {
            var functionId = lastFunctionId++;
            functions.Add (functionId, function);
            return functionId;
        }

        internal object InvokeFunction (int functionId, dynamic thisArg, dynamic arguments)
            => functions [functionId].Apply (thisArg, arguments);

        #endregion
    }
}