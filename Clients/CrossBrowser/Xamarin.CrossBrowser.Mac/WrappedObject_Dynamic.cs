//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Dynamic;

namespace Xamarin.CrossBrowser
{
    public partial class WrappedObject : DynamicObject
    {
        public override bool TryInvokeMember (InvokeMemberBinder binder, object[] args, out object result)
        {
            if (!HasProperty (binder.Name)) {
                result = null;
                return false;
            }

            result = Invoke (binder.Name, args);
            return true;
        }

        public override bool TryGetMember (GetMemberBinder binder, out object result)
        {
            if (!HasProperty (binder.Name)) {
                result = null;
                return false;
            }

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
            if (indexes.Length > 1)
                throw new ArgumentException ("multiples not supported", nameof (indexes));

            // FIXME: support string indexers
            result = GetValueAt ((int)indexes [0]);
            return true;
        }

        public override bool TryInvoke (InvokeBinder binder, object [] args, out object result)
        {
            result = Call (args);
            return true;
        }
    }
}