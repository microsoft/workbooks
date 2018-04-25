//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

using Newtonsoft.Json;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    sealed class DictionaryInteractiveObject : InteractiveObject
    {
        [JsonIgnore] readonly List<Tuple<RepresentedMemberInfo, object>> values
            = new List<Tuple<RepresentedMemberInfo, object>> ();

        public string Title { get; }

        [JsonConstructor]
        DictionaryInteractiveObject (
            string title,
            long handle,
            long representedObjectHandle,
            RepresentedType representedType,
            int depth,
            bool hasMembers,
            RepresentedMemberInfo [] members,
            object [] values,
            string toStringRepresentation,
            bool suppressToStringRepresentation)
            : base (
                handle,
                representedObjectHandle,
                representedType,
                depth,
                hasMembers,
                members,
                values,
                toStringRepresentation,
                suppressToStringRepresentation)
        {
            Title = title;
        }

        public DictionaryInteractiveObject (int depth, InteractiveItemPreparer itemPreparer,
            string title = null)
            : base (depth, itemPreparer)
        {
            Title = title;
        }

        public override void Initialize ()
        {
            base.Initialize ();
            Interact (false, null);
        }

        public void Add (MemberInfo memberInfo, object value, bool wrapAsMemberValueError = false)
        {
            if (wrapAsMemberValueError && value is Exception) {
                var e = (Exception)value;
                if (value is TargetInvocationException)
                    value = new GetMemberValueError (e.InnerException);
                else
                    value = new GetMemberValueError (e);
            }

            values.Add (Tuple.Create (new RepresentedMemberInfo (memberInfo), value));
        }

        protected override void Prepare ()
        {
            HasMembers = values.Count > 0;
        }

        protected override void ReadMembers ()
        {
            Members = new RepresentedMemberInfo [values.Count];
            Values = new object [values.Count];

            for (int i = 0; i < values.Count; i++) {
                var entry = values [i];

                var value = new RepresentedObject (entry.Item2?.GetType ());
                ItemPreparer (value, Depth + 1, entry.Item2);
                if (value.Count == 0)
                    value = null;

                Members [i] = entry.Item1;
                Values [i] = value;
            }
        }
    }
}