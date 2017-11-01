//
// ScriptFunction.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Xamarin.CrossBrowser
{
    [ComVisible (true)]
    [ClassInterface (ClassInterfaceType.None)]
    public class ScriptFunction : IReflect
    {
        readonly ScriptContext context;
        readonly ScriptAction action;
        readonly ScriptFunc func;

        protected ScriptFunction (ScriptContext context)
        {
            if (context == null)
                throw new ArgumentNullException (nameof(context));

            this.context = context;
        }

        public ScriptFunction (ScriptContext context, ScriptAction action) : this (context)
        {
            if (action == null)
                throw new ArgumentNullException (nameof(action));

            this.action = action;
        }

        public ScriptFunction (ScriptContext context, ScriptFunc func) : this (context)
        {
            if (func == null)
                throw new ArgumentNullException (nameof(func));

            this.func = func;
        }

        protected virtual dynamic Invoke (dynamic thisObject, dynamic [] arguments)
        {
            if (action != null) {
                action (thisObject, arguments);
                return null;
            }

            if (func != null)
                return func (thisObject, arguments);

            throw new NotImplementedException ();
        }

        #region Exported API

        // this API will be exposed to JS, but the implementations of the members should
        // be static and no-op since they will never be invoked - all COM operations
        // go through InvokeMember - the static member definitions are just to access
        // a MemberInfo to return (so we don't have to subclass MethodInfo, etc.)

        const BindingFlags staticFlags = BindingFlags.NonPublic | BindingFlags.Static;

        static void apply () { }
        static void call () { }

        static readonly MethodInfo [] methods = new [] { "apply", "call" }
            .Select (name => typeof (ScriptFunction).GetMethod (name, staticFlags))
            .ToArray ();

        MethodInfo [] IReflect.GetMethods (BindingFlags bindingAttr) => methods;
        PropertyInfo [] IReflect.GetProperties (BindingFlags bindingAttr) => null;
        FieldInfo [] IReflect.GetFields (BindingFlags bindingAttr) => null;

        #endregion

        #region Invoke Proxy

        public object Apply (dynamic thisArg, dynamic arguments)
        {
            dynamic applyArgs = context.FromComObject (arguments);
            var convertedArgs = new object [applyArgs.length];
            for (int i = 0; i < convertedArgs.Length; i++)
                convertedArgs [i] = context.FromComObject (applyArgs [i]);
            return context.ToComObject (Invoke (context.FromComObject (thisArg), convertedArgs));
        }

        public object Call (dynamic thisArg, params object [] arguments)
        {
            var convertedArgs = new object [arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                convertedArgs [i] = context.FromComObject (arguments [i]);
            return context.ToComObject (Invoke (context.FromComObject (thisArg), convertedArgs));
        }

        object IReflect.InvokeMember (string name, BindingFlags invokeAttr, Binder binder,
            object target, object [] args, ParameterModifier [] modifiers,
            CultureInfo culture, string [] namedParameters)
        {
            if (name == "apply")
                return Apply (args [0], args [1]);

            // [DispID=0] and call have the same format
            return Call (target, args);
        }

        #endregion

        #region IReflect Not Implemented

        Type IReflect.UnderlyingSystemType {
            get { throw new NotImplementedException (); }
        }

        FieldInfo IReflect.GetField (string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException ();
        }

        MemberInfo [] IReflect.GetMember (string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException ();
        }

        MemberInfo [] IReflect.GetMembers (BindingFlags bindingAttr)
        {
            throw new NotImplementedException ();
        }

        MethodInfo IReflect.GetMethod (string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException ();
        }

        MethodInfo IReflect.GetMethod (string name, BindingFlags bindingAttr, Binder binder,
            Type [] types, ParameterModifier [] modifiers)
        {
            throw new NotImplementedException ();
        }

        PropertyInfo IReflect.GetProperty (string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException ();
        }

        PropertyInfo IReflect.GetProperty (string name, BindingFlags bindingAttr, Binder binder,
            Type returnType, Type [] types, ParameterModifier [] modifiers)
        {
            throw new NotImplementedException ();
        }

        #endregion
    }
}