//
// AggregateObservableCollection.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Xamarin.Interactive.Collections
{
	public class AggregateObservableCollection<T> :
		IReadOnlyList<T>,
		INotifyPropertyChanging,
		INotifyPropertyChanged,
		INotifyCollectionChanged
	{
		readonly List<IReadOnlyList<T>> sources = new List<IReadOnlyList<T>> ();

		public event PropertyChangingEventHandler PropertyChanging;
		public event PropertyChangedEventHandler PropertyChanged;
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		void HandleSourcePropertyChanging (object sender, PropertyChangingEventArgs e)
		{
			switch (e.PropertyName) {
			case "Item[]":
			case "Count":
				PropertyChanging?.Invoke (this, e);
				break;
			}
		}

		void HandleSourcePropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName) {
			case "Item[]":
			case "Count":
				PropertyChanged?.Invoke (this, e);
				break;
			}
		}

		void HandleSourceCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke (
				this,
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
		}

		public void AddSource (IReadOnlyList<T> source)
		{
			var notifyPropertyChanging = source as INotifyPropertyChanging;
			if (notifyPropertyChanging != null)
				notifyPropertyChanging.PropertyChanging += HandleSourcePropertyChanging;

			var notifyPropertyChanged = source as INotifyPropertyChanged;
			if (notifyPropertyChanged != null)
				notifyPropertyChanged.PropertyChanged += HandleSourcePropertyChanged;

			var notifyCollectionChanged = source as INotifyCollectionChanged;
			if (notifyCollectionChanged != null)
				notifyCollectionChanged.CollectionChanged += HandleSourceCollectionChanged;

			sources.Add (source);

			CollectionChanged?.Invoke (
				this,
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
		}

		public T this [int index] {
			get {
				if (index >= 0) {
					foreach (var source in sources) {
						var count = source.Count;
						if (index < count)
							return source [index];
						index -= count;
					}
				}

				throw new ArgumentOutOfRangeException (
					nameof (index),
					"Index was out of range. Must be non-negative and " +
					"less than the size of the collection.");
			}
		}

		public int Count => sources.Sum (source => source.Count);

		public IEnumerator<T> GetEnumerator ()
		{
			foreach (var source in sources) {
				foreach (var item in source)
					yield return item;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
			=> GetEnumerator ();
	}
}