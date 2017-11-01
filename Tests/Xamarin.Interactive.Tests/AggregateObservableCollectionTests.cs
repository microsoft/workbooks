//
// AggregateObservableCollectionTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Collections;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class AggregateObservableCollectionTests
	{
		[Test]
		public void IndexerAndEnumerator ()
		{
			// this seed will produce a number of sources that have zero or one items
			var rand = new Random (1319791128);
			var expected = new List<Guid> ();
			var actual = new AggregateObservableCollection<Guid> ();

			for (int i = 0; i < 1000; i++) {
				var source = new List<Guid> ();
				actual.AddSource (source);

				for (int j = 0, n = rand.Next (0, 100); j < n; j++) {
					var guid = Guid.NewGuid ();
					expected.Add (guid);
					source.Add (guid);
				}
			}

			actual.Count.ShouldEqual (expected.Count);

			int index = 0;
			foreach (var item in actual) {
				item.ShouldEqual (expected [index]);
				actual [index].ShouldEqual (expected [index]);
				index++;
			}
		}
	}
}