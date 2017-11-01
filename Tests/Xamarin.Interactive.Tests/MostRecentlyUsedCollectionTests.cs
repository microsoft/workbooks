//
// MostRecentlyUsedCollectionTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Collections;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	sealed class MostRecentlyUsedCollectionTests
	{
		[Test]
		public void OrderAndDuplicates ()
		{
			var c = new MostRecentlyUsedCollection<string> { "a", "b", "c" };
			c.SequenceShouldEqual (new [] { "c", "b", "a" });

			c.Add ("b");
			c.SequenceShouldEqual (new [] { "b", "c", "a" });

			c.Add ("b");
			c.SequenceShouldEqual (new [] { "b", "c", "a" });
		}

		[Test]
		public void Maximum ()
		{
			var c = new MostRecentlyUsedCollection<string> ();

			var documents = Enumerable
				.Range (0, c.MaxCount * 2)
				.Select (i => i.ToString ())
				.ToArray ();

			documents.ForEach (c.Add);

			c.ShouldEqual (documents.Reverse ().Take (c.MaxCount));
		}

		[Test]
		public void Clear ()
		{
			var c = new MostRecentlyUsedCollection<string> { "a", "b", "c" };

			c.Count.ShouldEqual (3);

			c.Clear ();

			c.Count.ShouldEqual (0);
		}

		[Test]
		public void Load ()
		{
			var items = new [] { "a", "b", "c", "d" };
			var c = new MostRecentlyUsedCollection<string> ();
			c.Load (items);
			c.SequenceShouldEqual (items);
		}

		[Test]
		public void Validation ()
		{
			var validItems = new [] { "a", "b", "c" };

			var c = new MostRecentlyUsedCollection<string> (
				itemValidationDelegate: i => validItems.Contains (i));

			c.Load (new [] { "a", "b", "c", "d" });
			c.SequenceShouldEqual (validItems);

			c.Clear ();
			c.Count.ShouldEqual (0);

			c.Add ("d");
			c.Count.ShouldEqual (0);

			// ensure even if we make no inserts, we still validate the rest
			c.Load (new [] { "a", "b", "c", "d" });
			validItems = new [] { "a" };
			c.Add ("a");
			c.SequenceShouldEqual (new [] { "a" });
		}

		[Test]
		public void CollectionChanges ()
		{
			var expectedChanges = new List<NotifyCollectionChangedEventArgs> ();
			var actualChanges = new List<NotifyCollectionChangedEventArgs> ();

			var c = new MostRecentlyUsedCollection<string> (maxCount: 5);
			c.CollectionChanged += (sender, e) => {
				expectedChanges.Count.ShouldBeGreaterThan (0);
				actualChanges.Add (e);
			};

			void AssertChanges (Action action, params NotifyCollectionChangedEventArgs [] _expectedChanges)
			{
				actualChanges.Clear ();
				expectedChanges.Clear ();
				expectedChanges.AddRange (_expectedChanges);

				action ();

				actualChanges.Count.ShouldEqual (expectedChanges.Count);

				for (int i = 0; i < actualChanges.Count; i++)
					actualChanges [i].ShouldEqual (expectedChanges [i]);
			}

			// reset
			AssertChanges (
				() => {
					c.Load (new [] { "a", "b", "c", "d" });
					c.Count.ShouldEqual (4);
				},
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));

			// nothing ('a' was already at position 0)
			AssertChanges (
				() => {
					c.Add ("a");
					c.Count.ShouldEqual (4);
				});

			// move ('b' moved to position 0)
			AssertChanges (
				() => {
					c.Add ("b");
					c.Count.ShouldEqual (4);
				},
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Move, "b", 0, 1));

			// add ('e' inserted at position 0)
			AssertChanges (
				() => {
					c.Add ("e");
					c.Count.ShouldEqual (5);
				},
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, "e", 0));

			// reset
			AssertChanges (
				() => {
					c.Clear ();
					c.Count.ShouldEqual (0);
				},
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));

			// reset
			AssertChanges (
				() => {
					c.Load (new [] { "a", "b", "c", "d", "e" });
					c.Count.ShouldEqual (5);
				},
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));

			// add ('f' inserted at position 0, 'e' removed at position 5)
			AssertChanges (
				() => {
					c.Add ("f");
					c.Count.ShouldEqual (5);
				},
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, "f", 0),
				new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, "e", 5));
		}
	}
}