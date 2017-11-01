//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    sealed class ReflectionInteractiveObject : InteractiveObject
    {
        [NonSerialized] readonly object representedObject;
        [NonSerialized] readonly RepresentedMemberPredicate memberFilter;

        public ReflectionInteractiveObject (int depth,
            object representedObject,
            InteractiveItemPreparer itemPreparer,
            RepresentedMemberPredicate memberFilter = null)
            : base (depth, itemPreparer)
        {
            if (representedObject == null)
                throw new ArgumentNullException (nameof(representedObject));

            this.representedObject = representedObject;
            this.memberFilter = memberFilter;
            RepresentedObjectHandle = ObjectCache.Shared.GetHandle (representedObject);

            RepresentedType = RepresentedType.Lookup (representedObject.GetType ());
        }

        protected override void Prepare ()
        {
            var toString = representedObject.ToString ();
            if (toString != representedObject.GetType ().ToString ())
                ToStringRepresentation = toString;
            else
                SuppressToStringRepresentation = true;

            for (var type = RepresentedType; type != null && !HasMembers; type = type.BaseType)
                HasMembers = type.ProxyableMembers.Count > 0;
        }

        protected override void ReadMembers ()
        {
            try {
                NativeExceptionHandler.Trap ();
                SafeReadMembers ();
            } finally {
                NativeExceptionHandler.Release ();
            }
        }

        void SafeReadMembers ()
        {
            var propertyNames = new HashSet<string> ();
            var members = new List<Tuple<RepresentedMemberInfo, object>> ();

            for (var type = RepresentedType; type != null; type = type.BaseType) {
                foreach (var memberItem in type.ProxyableMembers) {
                    var pi = memberItem.Value.ResolvedMemberInfo as PropertyInfo;
                    if (pi != null) {
                        if (propertyNames.Contains (pi.Name))
                            continue;

                        propertyNames.Add (pi.Name);
                    }

                    object value;
                    try {
                        if (memberFilter != null &&
                            !memberFilter (memberItem.Value, representedObject))
                            value = new GetMemberValueError ();
                        else
                            value = memberItem.Value.GetValue (representedObject);
                    } catch (TargetInvocationException e) {
                        value = new GetMemberValueError (e.InnerException);
                    } catch (Exception e) {
                        value = new GetMemberValueError (e);
                    }

                    var preparedValue = new RepresentedObject (value?.GetType ());
                    ItemPreparer (preparedValue, Depth + 1, value);
                    if (preparedValue.Count == 0)
                        preparedValue = null;
                        
                    members.Add (Tuple.Create (memberItem.Value, (object)preparedValue));
                }
            }

            members.Sort ((x, y) => string.Compare (x.Item1.Name, y.Item1.Name));

            Members = new RepresentedMemberInfo [members.Count];
            Values = new object [members.Count];

            for (int i = 0; i < members.Count; i++) {
                Members [i] = members [i].Item1;
                Values [i] = members [i].Item2;
            }
        }
    }
}