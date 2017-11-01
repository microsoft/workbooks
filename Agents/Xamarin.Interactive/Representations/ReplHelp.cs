//
// ReplHelp.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;

using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    public sealed class ReplHelp : IRepresentationObject, IEnumerable<ReplHelp.Item>
    {
        [Serializable]
        public struct Item : IRepresentationObject
        {
            public ITypeMember Member { get; }
            public string Description { get; }
            public bool ShowReturnType { get; }

            public Item (ITypeMember member, string description, bool showReturnType = false)
            {
                Member = member;
                Description = description;
                ShowReturnType = showReturnType;
            }

            void ISerializableObject.Serialize (ObjectSerializer serializer)
            {
                throw new NotImplementedException ();
            }
        }

        List<Item> items = new List<Item> ();
        public IReadOnlyList<Item> Items {
            get { return items; }
        }

        public void Add (Item item)
        {
            items.Add (item);
        }

        public IEnumerator<Item> GetEnumerator ()
        {
            return items.GetEnumerator ();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        void ISerializableObject.Serialize (ObjectSerializer serializer)
        {
            throw new NotImplementedException ();
        }
    }
}