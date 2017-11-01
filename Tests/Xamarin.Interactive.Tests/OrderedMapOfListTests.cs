//
// OrderedMapOfListTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using Should;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class OrderedMapOfListTests
	{
		[Test]
		public void Add ()
		{
			var map = new OrderedMapOfList<string, string> ();
			map.Count.ShouldEqual (0);
			map.Add ("a", "one");
			map.Count.ShouldEqual (1);
			map.Add ("a", "two");
			map.Count.ShouldEqual (1);
			map.Add ("b", "one");
			map.Count.ShouldEqual (2);
			map.Add ("b", "two");
			map.Count.ShouldEqual (2);
		}

		OrderedMapOfList<string, string> CreateMap ()
			=> new OrderedMapOfList<string, string> {
				// a: one two three
				// b: one two
				// c: one
				{ "a", "one" },
				{ "b", "one" },
				{ "c", "one" },
				{ "a", "two" },
				{ "b", "two" },
				{ "a", "three" }
			};

		[Test]
		public void EnumerateKeys ()
			=> CreateMap ().Keys.ShouldEqual (new [] { "a", "b", "c" });

		[Test]
		public void EnumerateValues ()
		{
			var map = CreateMap ();
			map.Values.ElementAt (0).ShouldEqual (new [] { "one", "two", "three" });
			map.Values.ElementAt (1).ShouldEqual (new [] { "one", "two" });
			map.Values.ElementAt (2).ShouldEqual (new [] { "one" });
		}

		[Test]
		public void Enumerate ()
		{
			var map = CreateMap ();
			map.ElementAt (0).Key.ShouldEqual ("a");
			map.ElementAt (0).Value.ShouldEqual (new [] { "one", "two", "three" });
			map.ElementAt (1).Key.ShouldEqual ("b");
			map.ElementAt (1).Value.ShouldEqual (new [] { "one", "two" });
			map.ElementAt (2).Key.ShouldEqual ("c");
			map.ElementAt (2).Value.ShouldEqual (new [] { "one" });
		}

		[Test]
		public void Get ()
		{
			var map = CreateMap ();
			map ["a"].ShouldEqual (new [] { "one", "two", "three" });
			map ["b"].ShouldEqual (new [] { "one", "two" });
			map ["c"].ShouldEqual (new [] { "one" });
			Assert.Throws<KeyNotFoundException> (() => map ["d"].ShouldBeNull ());
		}

		[Test]
		public void Count ()
			=> CreateMap ().Count.ShouldEqual (3);

		[Test]
		public void Contains ()
		{
			var map = CreateMap ();
			map.ContainsKey ("a").ShouldBeTrue ();
			map.ContainsKey ("b").ShouldBeTrue ();
			map.ContainsKey ("c").ShouldBeTrue ();
			map.ContainsKey ("d").ShouldBeFalse ();
		}

		[Test]
		public void TryGetValue ()
		{
			var map = CreateMap ();

			IReadOnlyList<string> list;

			map.TryGetValue ("a", out list).ShouldBeTrue ();
			list.ShouldEqual (new [] { "one", "two", "three" });
			map ["a"].ShouldBeSameAs (list);

			map.TryGetValue ("b", out list).ShouldBeTrue ();
			list.ShouldEqual (new [] { "one", "two" });
			map ["b"].ShouldBeSameAs (list);

			map.TryGetValue ("c", out list).ShouldBeTrue ();
			list.ShouldEqual (new [] { "one" });
			map ["c"].ShouldBeSameAs (list);

			map.TryGetValue ("d", out list).ShouldBeFalse ();
			list.ShouldBeNull ();
		}
	}
}