//
// RepresentationTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	class RepresentationTests
	{
		RepresentationManager manager;

		[SetUp]
		public void SetUp ()
		{
			manager = new RepresentationManager ();
		}

		#region RepresentationManager.Normalize

		// Types that all agents support without explicit representation boxing

		[Flags]
		enum FlagsEnum
		{
			Zero = 0,
			One = 1,
			Two = 2,
			Four = 4,
			Eight = 8
		}

		enum SByteEnum : sbyte { Min = sbyte.MinValue, Max = sbyte.MaxValue }
		enum ByteEnum : byte { Min = byte.MinValue, Max = byte.MaxValue }
		enum Int16Enum : short { Min = short.MinValue, Max = short.MaxValue }
		enum UInt16Enum : ushort { Min = ushort.MinValue, Max = ushort.MaxValue }
		enum Int32Enum : int { Min = int.MinValue, Max = int.MaxValue }
		enum UInt32Enum : uint { Min = uint.MinValue, Max = uint.MaxValue }
		enum Int64Enum : long { Min = long.MinValue, Max = long.MaxValue }
		enum UInt64Enum : ulong { Min = ulong.MinValue, Max = ulong.MaxValue }

		[TestCase (FlagsEnum.Zero)]
		[TestCase (FlagsEnum.One)]
		[TestCase (FlagsEnum.Two)]
		[TestCase (FlagsEnum.Four)]
		[TestCase (FlagsEnum.Eight)]
		[TestCase (SByteEnum.Min)]
		[TestCase (SByteEnum.Max)]
		[TestCase (ByteEnum.Min)]
		[TestCase (ByteEnum.Max)]
		[TestCase (Int16Enum.Min)]
		[TestCase (Int16Enum.Max)]
		[TestCase (UInt16Enum.Min)]
		[TestCase (UInt16Enum.Max)]
		[TestCase (Int32Enum.Min)]
		[TestCase (Int32Enum.Max)]
		[TestCase (UInt32Enum.Min)]
		[TestCase (UInt32Enum.Max)]
		[TestCase (Int64Enum.Min)]
		[TestCase (Int64Enum.Max)]
		[TestCase (UInt64Enum.Min)]
		[TestCase (UInt64Enum.Max)]
		public void EnumValue (Enum value)
		{
			var reps = manager.Prepare (value);

			reps.Count.ShouldEqual (2);

			var rep = reps [0].ShouldBeInstanceOf<EnumValue> ();

			var type = value.GetType ();
			var underlyingType = type.GetEnumUnderlyingType ();

			rep.RepresentedType.ShouldEqual (reps.RepresentedType);
			rep.RepresentedType.Name.ShouldEqual (type.ToString ());

			rep.UnderlyingType.ResolvedType.ShouldEqual (underlyingType);

			rep.IsFlags.ShouldEqual (type.IsDefined (typeof (FlagsAttribute), false));

			rep.Value.ShouldEqual (Convert.ChangeType (
				value,
				underlyingType,
				CultureInfo.InvariantCulture));

			rep.Names.ShouldEqual (Enum.GetNames (type));

			var values = Enum.GetValues (type);

			rep.Values.Length.ShouldEqual (values.Length);

			for (int i = 0; i < values.Length; i++)
				rep.Values [i].ShouldEqual (Convert.ChangeType (
					values.GetValue (i),
					underlyingType,
					CultureInfo.InvariantCulture));
		}

		[Test]
		public void IInteractiveObject ()
		{
			var interactiverObject = new DictionaryInteractiveObject (0, manager.Prepare);
			var reps = manager.Prepare (interactiverObject);
			reps.Count.ShouldEqual (2);
			reps [0].ShouldBeInstanceOf<DictionaryInteractiveObject> ();
		}

		[Test]
		public void ExceptionNode ()
		{
			foreach (var exception in new Exception [] {
				new Exception ("test exception"),
				new Exception ("outer", new Exception ("inner"))
			}) {
				var reps = manager.Prepare (exception);
				reps.Count.ShouldEqual (3);
				var rep = reps [0].ShouldBeInstanceOf<ExceptionNode> ();
				reps [2].ShouldBeInstanceOf<ReflectionInteractiveObject> ();
				rep.Message.ShouldEqual (exception.Message);
			}
		}

		public string MemberProperty { get; set; }

		[Test]
		public void MemberInfo ()
		{
			var member = GetType ().GetProperty (nameof (MemberProperty));

			var reps = manager.Prepare (member);

			reps.Count.ShouldEqual (3);

			var rep = reps [0].ShouldBeInstanceOf<Property> ();
			rep.Name.ShouldEqual (member.Name);
			rep.DeclaringType.Name.Namespace.ShouldEqual (member.DeclaringType.Namespace);
			rep.DeclaringType.Name.Name.ShouldEqual (member.DeclaringType.Name);
			rep.PropertyType.Name.Namespace.ShouldEqual (member.PropertyType.Namespace);
			rep.PropertyType.Name.Name.ShouldEqual (member.PropertyType.Name);
			rep.Getter.ShouldNotBeNull ();
			rep.Setter.ShouldNotBeNull ();

			reps [2].ShouldBeInstanceOf<ReflectionInteractiveObject> ();
		}

		#endregion

		#region RepresentationManager.Prepare: raw types

		// Raw types do not need to be boxed into serialization-safe containers.
		// All supported runtimes can directly handle the types in compatible ways.

		sealed class EditAllTheThingsRepresentationProvider : RepresentationProvider
		{
			public override IEnumerable<object> ProvideRepresentations (object obj)
			{
				yield return new Representation (obj, canEdit: true);
			}
		}

		void AssertRaw (Array values, bool hasInteractiveRepresentation)
		{
			for (int i = 0; i < values.Length; i++)
				AssertRaw (values.GetValue (i), hasInteractiveRepresentation);
		}

		void AssertRaw (object value, bool hasInteractiveRepresentation)
		{
			value.ShouldNotBeNull ();

			manager = new RepresentationManager (); // [SetUp] doesn't know we're looping

			for (int i = 0; i < 2; i++) {
				if (i == 1)
					manager.AddProvider (new EditAllTheThingsRepresentationProvider ());

				var valueType = value.GetType ();
				var reps = manager.Prepare (value);

				reps.Count.ShouldEqual (hasInteractiveRepresentation ? 3 : 2);
				reps.RepresentedType.ResolvedType.ShouldEqual (valueType);

				reps [0].GetType ().ShouldEqual (valueType);
				reps [0].ShouldEqual (value);

				reps.GetRepresentation (0).CanEdit.ShouldEqual (i == 1);
			}
		}

		[TestCase (true)]
		[TestCase (false)]
		[TestCase (Char.MinValue, TestName = "Char.MinValue")]
		[TestCase (Char.MaxValue, TestName = "Char.MaxValue")]
		[TestCase (SByte.MinValue)]
		[TestCase (SByte.MaxValue)]
		[TestCase (Byte.MinValue)]
		[TestCase (Byte.MaxValue)]
		[TestCase (Int16.MinValue)]
		[TestCase (UInt16.MaxValue)]
		[TestCase (Int32.MinValue)]
		[TestCase (UInt32.MaxValue)]
		[TestCase (Int64.MinValue)]
		[TestCase (UInt64.MaxValue)]
		[TestCase (Single.MinValue)]
		[TestCase (Single.MaxValue)]
		[TestCase (Single.Epsilon)]
		[TestCase (Single.PositiveInfinity)]
		[TestCase (Single.NegativeInfinity)]
		[TestCase (Single.NaN)]
		[TestCase (Double.MinValue)]
		[TestCase (Double.MaxValue)]
		[TestCase (Double.Epsilon)]
		[TestCase (Double.PositiveInfinity)]
		[TestCase (Double.NegativeInfinity)]
		[TestCase (Double.NaN)]
		[TestCase (Math.PI)]
		[TestCase (Math.E)]
		public void Raw_TypeCode_Constable (object value)
			=> AssertRaw (value, hasInteractiveRepresentation: false);

		[TestCase ("")]
		[TestCase ("🙀 🐖 💨")]
		public void Raw_TypeCode_String (string value)
			=> AssertRaw (value, hasInteractiveRepresentation: true);

		[Test]
		public void Raw_TypeCode_DateTime ()
			=> AssertRaw (
				new [] {
					DateTime.MinValue,
					DateTime.MaxValue,
					DateTime.Now,
					DateTime.UtcNow,
					DateTime.Today
				},
				hasInteractiveRepresentation: true);

		[Test]
		public void Raw_TypeCode_Decimal ()
			=> AssertRaw (
				new [] {
					Decimal.MinValue,
					Decimal.MaxValue,
					Decimal.Zero,
					Decimal.MinusOne,
					Decimal.One
				},
				hasInteractiveRepresentation: false);

		[Test]
		public void Raw_TimeSpan ()
			=> AssertRaw (
				new [] {
					TimeSpan.Zero,
					TimeSpan.MinValue,
					TimeSpan.MaxValue
				},
				hasInteractiveRepresentation: true);

		[Test]
		public void Raw_Guid ()
			=> AssertRaw (
				new [] {
					Guid.Empty,
					Guid.NewGuid ()
				},
				hasInteractiveRepresentation: true);

		#endregion

		#region RepresentationManager.Prepare: WordSizedNumber

		void AssertWordSizedNumber<T> (T [] values, WordSizedNumberFlags expectedFlags)
		{
			manager = new RepresentationManager (); // [SetUp] doesn't know we're looping
			//#if MAC || IOS
			//manager.AddProvider (new Unified.UnifiedRepresentationProvider ());
			//#endif

			for (int i = 0; i < values.Length; i++) {
				var reps = manager.Prepare (values [i]);
				reps.Count.ShouldEqual (2);
				var word = reps [0].ShouldBeInstanceOf<WordSizedNumber> ();
				word.Flags.ShouldEqual (expectedFlags);
				word.Value.ToString ().ShouldEqual (values [i].ToString ());
			}
		}

		[Test]
		public void WordSizedNumber_IntPtr ()
			=> AssertWordSizedNumber (
				new [] {
					IntPtr.Zero,
					(IntPtr)(IntPtr.Size == 4 ? Int32.MinValue : Int64.MinValue),
					(IntPtr)(IntPtr.Size == 4 ? Int32.MaxValue : Int64.MaxValue)
				},
				WordSizedNumberFlags.Pointer | WordSizedNumberFlags.Signed);

		[Test]
		public void WordSizedNumber_UIntPtr ()
			=> AssertWordSizedNumber (
				new [] {
					UIntPtr.Zero,
					(UIntPtr)(UIntPtr.Size == 4 ? UInt32.MinValue : UInt64.MinValue),
					(UIntPtr)(UIntPtr.Size == 4 ? UInt32.MaxValue : UInt64.MaxValue)
				},
				WordSizedNumberFlags.Pointer);

		#if false && (MAC || IOS)

		[Test]
		public void WordSizedNumber_NInt ()
			=> AssertWordSizedNumber (
				new [] {
					nint.MinValue,
					nint.MaxValue
				},
				WordSizedNumberFlags.Signed);

		[Test]
		public void WordSizedNumber_NUint ()
			=> AssertWordSizedNumber (
				new [] {
					nuint.MinValue,
					nuint.MaxValue
				},
				WordSizedNumberFlags.None);

		[Test]
		public void WordSizedNumber_NFloat ()
			=> AssertWordSizedNumber (
				new [] {
					nfloat.MinValue,
					nfloat.MaxValue,
					nfloat.Epsilon,
					nfloat.PositiveInfinity,
					nfloat.NegativeInfinity,
					nfloat.NaN
				},
				WordSizedNumberFlags.Real);

		#endif

		#endregion

		[TestCase (true)]
		[TestCase (false)]
		public void RootObject_AllowISerializableObject (bool allowISerializableObject)
		{
			var reps = manager.Prepare (new Color (0.5, 1, 0.25, 0.3), allowISerializableObject);

			reps.Count.ShouldEqual (3);

			if (allowISerializableObject)
				reps [0].ShouldBeInstanceOf<JsonPayload> ();
			else
				reps [0].ShouldBeInstanceOf<Color> ();

			reps [reps.Count - 1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();
		}

		[TestCase (true)]
		[TestCase (false)]
		public void ChildObject_AllowISerializableObject (bool allowISerializableObject)
		{
			var reps = manager.Prepare (new {
				Color = new Color (0.5, 1, 0.25, 0.3),
				Point = new Point (10, 20),
				String = "hello"
			}, allowISerializableObject);

			reps.Count.ShouldEqual (2);

			reps [1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();

			var root = (ReflectionInteractiveObject)reps [1];
			root.Interact (new InteractiveObject.ReadAllMembersInteractMessage ());

			root.Members.ShouldNotBeNull ();
			root.Values.ShouldNotBeNull ();

			root.Members.Length.ShouldEqual (3);
			root.Values.Length.ShouldEqual (3);

			var colorRep = root.Values [0].ShouldBeInstanceOf<RepresentedObject> ();
			colorRep.Count.ShouldEqual (3);
			if (allowISerializableObject)
				colorRep [0].ShouldBeInstanceOf<JsonPayload> ();
			else
				colorRep [0].ShouldBeInstanceOf<Color> ();
			colorRep [colorRep.Count - 1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();

			var pointRep = root.Values [1].ShouldBeInstanceOf<RepresentedObject> ();
			pointRep.Count.ShouldEqual (3);
			pointRep [0].ShouldBeInstanceOf<Point> ();
			pointRep [pointRep.Count - 1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();

			var stringRep = root.Values [2].ShouldBeInstanceOf<RepresentedObject> ();
			stringRep.Count.ShouldEqual (3);
			stringRep [0].ShouldBeInstanceOf<string> ();
			stringRep [stringRep.Count - 1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();
		}
	}
}