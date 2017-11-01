//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;

namespace Xamarin.Interactive.Collections
{
    class MostRecentlyUsedCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged
    {
        readonly List<T> collection = new List<T> ();

        readonly Func<T, bool> itemValidationDelegate;
        readonly IEqualityComparer<T> equalityComparer;

        public int MaxCount { get; }

        public int Count => collection.Count;

        public T this [int index] => collection [index];

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public MostRecentlyUsedCollection (
            int maxCount = 10,
            Func<T, bool> itemValidationDelegate = null,
            IEqualityComparer<T> equalityComparer = null)
        {
            MaxCount = maxCount;

            this.itemValidationDelegate = itemValidationDelegate;
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        }

        protected virtual void OnCollectionChanged (NotifyCollectionChangedEventArgs args)
            => CollectionChanged?.Invoke (this, args);

        public void Clear ()
        {
            if (collection.Count > 0) {
                collection.Clear ();
                OnCollectionChanged (
                    new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
            }
        }

        public void Add (T item)
        {
            switch (collection.IndexOf (item, equalityComparer)) {
            case 0:
                break;
            case int i when i > 0:
                collection.RemoveAt (i);
                collection.Insert (0, item);
                OnCollectionChanged (
                    new NotifyCollectionChangedEventArgs (
                        NotifyCollectionChangedAction.Move,
                        item,
                        0,
                        i));
                break;
            default:
                collection.Insert (0, item);
                OnCollectionChanged (
                    new NotifyCollectionChangedEventArgs (
                        NotifyCollectionChangedAction.Add,
                        item,
                        0));
                break;
            }

            for (var i = collection.Count - 1; i >= 0; i--) {
                var removalItem = collection [i];
                if (i >= MaxCount || (itemValidationDelegate != null &&
                    !itemValidationDelegate (removalItem))) {
                    collection.RemoveAt (i);
                    OnCollectionChanged (
                            new NotifyCollectionChangedEventArgs (
                            NotifyCollectionChangedAction.Remove,
                            removalItem,
                            i));
                }
            }
        }

        public void Load (IEnumerable<T> documents)
        {
            if (itemValidationDelegate != null)
                documents = documents.Where (itemValidationDelegate);

            Clear ();

            collection.AddRange (documents);

            if (collection.Count > 0)
                OnCollectionChanged (
                    new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
        }

        public IEnumerator<T> GetEnumerator ()
            => collection.GetEnumerator ();

        IEnumerator IEnumerable.GetEnumerator ()
            => collection.GetEnumerator ();
    }
}