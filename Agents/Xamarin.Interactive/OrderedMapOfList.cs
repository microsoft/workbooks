//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Xamarin.Interactive
{
    /// <summary>
    /// An inefficient dictionary where keys are ordered (via insertion order) and insertions
    /// on duplicate keys results in values being appended to a list (one-to-many relationship).
    /// </summary>
    sealed class OrderedMapOfList<TKey, TListItemValue> : IReadOnlyDictionary<TKey, IReadOnlyList<TListItemValue>>
    {
        readonly IEqualityComparer<TKey> keyEqualityComparer;

        int count;
        List<TKey> keys;
        List<List<TListItemValue>> values;

        public OrderedMapOfList (IEqualityComparer<TKey> keyEqualityComparer = null)
        {
            this.keyEqualityComparer = keyEqualityComparer ?? EqualityComparer<TKey>.Default;
        }

        public void Add (TKey key, TListItemValue value)
        {
            if (keys == null) {
                count = 1;
                keys = new List<TKey> (4) { key };
                values = new List<List<TListItemValue>> (4) {
                    new List<TListItemValue> { value }
                };
                return;
            }

            var list = GetList (key);
            if (list != null) {
                list.Add (value);
                return;
            }

            count++;
            keys.Add (key);
            values.Add (new List<TListItemValue> { value });
        }

        List<TListItemValue> GetList (TKey key)
        {
            for (int i = 0; i < count; i++)
                if (keyEqualityComparer.Equals (keys [i], key))
                    return values [i];

            return null;
        }

        public int Count => count;

        public IReadOnlyList<TKey> Keys => keys ?? (IReadOnlyList<TKey>)Array.Empty<TKey> ();

        IEnumerable<TKey> IReadOnlyDictionary<TKey, IReadOnlyList<TListItemValue>>.Keys => Keys;

        public IReadOnlyList<IReadOnlyList<TListItemValue>> Values => values
            ?? (IReadOnlyList<IReadOnlyList<TListItemValue>>)Array.Empty<IReadOnlyList<TListItemValue>> ();

        IEnumerable<IReadOnlyList<TListItemValue>> IReadOnlyDictionary<TKey, IReadOnlyList<TListItemValue>>
            .Values => Values;

        public IReadOnlyList<TListItemValue> this [TKey key] {
            get {
                var value = GetList (key);
                if (value != null)
                    return value;

                throw new KeyNotFoundException ();
            }
        }

        public bool ContainsKey (TKey key) => GetList (key) != null;

        public bool TryGetValue (TKey key, out IReadOnlyList<TListItemValue> value)
        {
            value = GetList (key);
            return value != null;
        }

        public IEnumerator<KeyValuePair<TKey, IReadOnlyList<TListItemValue>>> GetEnumerator ()
        {
            for (var i = 0; i < count; i++)
                yield return new KeyValuePair<TKey, IReadOnlyList<TListItemValue>> (
                    keys [i],
                    values [i]);
        }

        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
    }
}