//
// ObjectSerializerTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using NUnit.Framework;
using Should;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	/// <summary>
	/// Tests for <see cref="ObjectSerializer"/>. These tests only ensure that the generated
	/// JSON is correct. They do not ensure deserialization/compat with Newtonsoft.Json.
	/// See the <see cref="RepresentationTests"/> for roundtripping, etc.
	/// </summary>
	public class ObjectSerializerTests
	{
		class Empty : ISerializableObject
		{
			public void Serialize (ObjectSerializer serializer)
			{
			}
		}

		[Test]
		public void EmptyObject ()
			=> new Empty ().SerializeToString (false, false).ShouldEqual ("{}");

		class SingleProperty : ISerializableObject
		{
			public void Serialize (ObjectSerializer serializer)
				=> serializer.Property ("a", true);
		}

		[Test]
		public void SinglePropertyObject ()
			=> new SingleProperty ().SerializeToString (false, false).ShouldEqual ("{\"a\":true}");

		class MultipleProperties : ISerializableObject
		{
			public void Serialize (ObjectSerializer serializer)
			{
				serializer.Property ("a", 10);
				serializer.Property ("b", "hello");
				serializer.Property ("c", Math.PI);
			}
		}

		[Test]
		public void MultiplePropertiesObject ()
			=> new MultipleProperties ().SerializeToString (false, false).ShouldEqual (
				"{\"a\":10,\"b\":\"hello\",\"c\":3.1415926535897931}");

		class Nested : ISerializableObject
		{
			class Depth2 : ISerializableObject
			{
				readonly int d;

				public Depth2 (int d)
				{
					this.d = d;
				}

				public void Serialize (ObjectSerializer serializer)
				{
					serializer.Property ("d-a", d);
					serializer.Property ("d-b", d);
				}
			}

			public void Serialize (ObjectSerializer serializer)
			{
				serializer.Property ("a", new Depth2 (1));
				serializer.Property ("b", new Depth2 (2));
				serializer.Property ("c", new Depth2 (3));
			}
		}

		[Test]
		public void NestedObject ()
			=> new Nested ().SerializeToString (false, false).ShouldEqual (
				"{\"a\":{\"d-a\":1,\"d-b\":1}," +
				"\"b\":{\"d-a\":2,\"d-b\":2}," +
				"\"c\":{\"d-a\":3,\"d-b\":3}}");

		struct EmptyStruct : ISerializableObject
		{
			public void Serialize (ObjectSerializer serializer)
			{
			}
		}

		class _DefaultMembers : ISerializableObject
		{
			public void Serialize (ObjectSerializer serializer)
			{
				serializer.Property ("EmptyStruct", new EmptyStruct ());
				serializer.Property ("NullString", null as string);
			}
		}

		[Test]
		public void DefaultMembers ()
			=> new _DefaultMembers ().SerializeToString (false, false).ShouldEqual ("{}");

		public class CycleObject : ISerializableObject
		{
			public CycleObject Cycle { get; set; }

			public void Serialize (ObjectSerializer serializer)
				=> serializer.Property (nameof (Cycle), this);
		}

		[Test]
		public void Cycles ()
		{
			var original = new CycleObject ();
			original.Cycle = original;

			var serialized = original.SerializeToString ();
			serialized.ShouldEqual (
				"{\"$type\":\"" + typeof (CycleObject).ToSerializableName () + "\"," +
				"\"$id\":\"0\",\"Cycle\":{\"$ref\":\"0\"}}");
		}
	}
}