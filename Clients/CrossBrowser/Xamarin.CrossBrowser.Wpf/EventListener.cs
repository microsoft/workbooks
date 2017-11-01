//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Xamarin.CrossBrowser
{
    [ComVisible (true)]
    [ClassInterface (ClassInterfaceType.AutoDispatch)]
    public class EventListener : IReflect
    {
        static T NotImplemented<T>()
        {
            throw new NotImplementedException ();
        }

        readonly ScriptContext context;
        readonly Action<Event> handler;

        internal EventListener (ScriptContext context, Action<Event> handler)
        {
            if (context == null)
                throw new ArgumentNullException (nameof (context));

            if (handler == null)
                throw new ArgumentNullException (nameof (handler));

            this.context = context;
            this.handler = handler;
        }

        object IReflect.InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target, object [] args,
            ParameterModifier [] modifiers, CultureInfo culture, string [] namedParameters)
        {
            handler (WrappedObject.Wrap<Event> (context, args [1]));
            return null;
        }

        Type IReflect.UnderlyingSystemType => typeof (object);

        FieldInfo [] IReflect.GetFields (BindingFlags bindingAttr) => new FieldInfo [0];
        MethodInfo [] IReflect.GetMethods (BindingFlags bindingAttr) => new MethodInfo [0];
        PropertyInfo [] IReflect.GetProperties (BindingFlags bindingAttr) => new PropertyInfo [0];

        FieldInfo IReflect.GetField (string name, BindingFlags bindingAttr) => NotImplemented<FieldInfo> ();
        MemberInfo [] IReflect.GetMember (string name, BindingFlags bindingAttr) => NotImplemented<MemberInfo []> ();
        MemberInfo [] IReflect.GetMembers (BindingFlags bindingAttr) => NotImplemented<MemberInfo []> ();
        MethodInfo IReflect.GetMethod (string name, BindingFlags bindingAttr) => NotImplemented<MethodInfo> ();

        MethodInfo IReflect.GetMethod (string name, BindingFlags bindingAttr, Binder binder,
            Type [] types, ParameterModifier [] modifiers)
            => NotImplemented<MethodInfo> ();

        PropertyInfo IReflect.GetProperty (string name, BindingFlags bindingAttr)
            => NotImplemented<PropertyInfo> ();

        PropertyInfo IReflect.GetProperty (string name, BindingFlags bindingAttr, Binder binder,
            Type returnType, Type [] types, ParameterModifier [] modifiers)
            => NotImplemented<PropertyInfo> ();
    }

    public class EventListener<TEvent> : EventListener where TEvent : Event
    {
        internal EventListener (ScriptContext context, Action<TEvent> handler) : base (context, e => handler ((TEvent)e))
        {
        }
    }
}