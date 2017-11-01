//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;

namespace Xamarin.Interactive.Collections
{
    class ObservableCollection<T> : System.Collections.ObjectModel.ObservableCollection<T>
    {
        readonly IEqualityComparer<T> equalityComparer;

        public ObservableCollection (IEqualityComparer<T> equalityComparer = null)
        {
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        }

        /// <summary>
        /// Updates the items of this instance to contain only and all of
        /// the items in <paramref name="items"/>.
        /// </summary>
        public void UpdateTo (IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0) {
                Clear ();
                return;
            }

            for (int i = 0; i < items.Count; i++) {
                var item = items [i];

                if (i < Count && equalityComparer.Equals (this [i], item))
                    // item and position in old and new list is the same, do nothing
                    continue;

                if (i >= Count) {
                    // add if we could never insert or move the item
                    Add (item);
                } else {
                    var oldIndex = IndexOf (item);
                    if (oldIndex >= 0)
                        // item was somewhere in the old list, move it
                        Move (oldIndex, i);
                    else
                        // not found, insert at our current point
                        Insert (i, item);
                }
            }

            // everything in the old list is stale, so remove
            while (Count > items.Count)
                RemoveAt (items.Count);
        }

        public new int IndexOf (T item)
            => this.IndexOf (item, equalityComparer);

        bool inhibitCollectionChangedEvent;

        protected override void OnCollectionChanged (NotifyCollectionChangedEventArgs e)
        {
            if (!inhibitCollectionChangedEvent)
                base.OnCollectionChanged (e);
        }

        public void AddRange (IEnumerable<T> children)
        {
            if (children != null) {
                var clone = new List<T> (children);
                var count = clone.Count;
                if (count == 0)
                    return;
                inhibitCollectionChangedEvent = true;
                for (int i = 0; i < count; i++)
                    Add (clone [i]);
                inhibitCollectionChangedEvent = false;
                OnCollectionChanged (new NotifyCollectionChangedEventArgs (
                    NotifyCollectionChangedAction.Add,
                    clone));
            }
        }
    }
}