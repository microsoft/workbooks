//
// ScriptObject_Dynamic.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Dynamic;
using System.Globalization;

namespace Xamarin.CrossBrowser
{
    public partial class WrappedObject : DynamicObject
    {
        public override bool TryInvokeMember (InvokeMemberBinder binder, object [] args, out object result)
        {
            result = Invoke (binder.Name, args);
            return true;
        }

        public override bool TryGetMember (GetMemberBinder binder, out object result)
        {
            result = GetProperty (binder.Name);
            return true;
        }

        public override bool TrySetMember (SetMemberBinder binder, object value)
        {
            SetProperty (binder.Name, value);
            return true;
        }

        public override bool TryGetIndex (GetIndexBinder binder, object [] indexes, out object result)
        {
            // FIXME: would really be nice to find another way!
            result = GetProperty (((int)indexes [0]).ToString (CultureInfo.InvariantCulture));
            return true;
        }

        public override bool TryInvoke (InvokeBinder binder, object [] args, out object result)
        {
            result = Context.Bridge.ApplyFunction (ComObject, args);
            return true;
        }
    }
}