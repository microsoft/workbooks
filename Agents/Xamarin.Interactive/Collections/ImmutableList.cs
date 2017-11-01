//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Xamarin.Interactive.Collections
{
    /// <summary>
	/// An intentionally simple, immutable list. To be replaced via NuGet/ILRepack when
	/// we can depend on System.Collections.Immutable or some other NuGet.
	/// </summary>
    sealed class ImmutableList<T> : IReadOnlyList<T>
    {
        public static readonly ImmutableList<T> Empty = new ImmutableList<T> (new List<T> ());

        readonly List<T> items;

        public int Count => items.Count;

        public T this [int index] => items [index];

        ImmutableList (List<T> items) => this.items = items;

        public ImmutableList<T> Add (T item)
        {
            var newItems = new List<T> (items.Count + 1);
            newItems.AddRange (items);
            newItems.Add (item);
            return new ImmutableList<T> (newItems);
        }

        public ImmutableList<T> AddRange (IEnumerable<T> items)
        {
            var newItems = new List<T> (this.items.Count + 1);
            newItems.AddRange (this.items);
            newItems.AddRange (items);
            return new ImmutableList<T> (newItems);
        }

        public ImmutableList<T> Insert (int index, T item)
        {
            var newItems = new List<T> (items.Count + 1);
            newItems.AddRange (items);
            newItems.Insert (index, item);
            return new ImmutableList<T> (newItems);
        }

        public ImmutableList<T> InsertRange (int index, IEnumerable<T> items)
        {
            var newItems = new List<T> (this.items.Count + 1);
            newItems.AddRange (this.items);
            newItems.InsertRange (index, items);
            return new ImmutableList<T> (newItems);
        }

        public int IndexOf (T item)
            => IndexOf (item, EqualityComparer<T>.Default);

        public int IndexOf (T item, IEqualityComparer<T> equalityComparer)
        {
            if (equalityComparer == null)
                throw new ArgumentNullException (nameof (equalityComparer));

            for (int i = 0; i < Count; i++) {
                if (equalityComparer.Equals (items [i], item))
                    return i;
            }

            return -1;
        }

        public ImmutableList<T> RemoveAt (int index)
        {
            var newItems = new List<T> (items.Count);
            newItems.AddRange (items);
            newItems.RemoveAt (index);
            return new ImmutableList<T> (newItems);
        }

        public ImmutableList<T> Remove (T item)
            => Remove (item, EqualityComparer<T>.Default);

        public ImmutableList<T> Remove (T item, IEqualityComparer<T> equalityComparer)
        {
            if (equalityComparer == null)
                throw new ArgumentNullException (nameof (equalityComparer));
            
            var index = IndexOf (item, equalityComparer);
            if (index < 0)
                return this;

            return RemoveAt (index);
        }

        public IEnumerator<T> GetEnumerator ()
        {
            for (int i = 0; i < Count; i++)
                yield return items [i];
        }

        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
    }
}