//
// PreferencesTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class PreferencesTests
	{
		[SetUp]
		public void TestFixtureSetUp () => PreferenceStore.Default.RemoveAll ();

		[Test]
		public void StringArrayPref ()
		{
			var pref = new Preference<string []> ("StringArrayPref", new [] { "default" });
			pref.GetValue ().ShouldEqual (new [] { "default" });
			pref.SetValue (new string [0]);
			pref.GetValue ().ShouldEqual (new string [0]);
			pref.SetValue (new [] { "one", "two", "three" });
			pref.GetValue ().ShouldEqual (new [] { "one", "two", "three" });
			pref.SetValue (new string [] { String.Empty });
			pref.GetValue ().ShouldEqual (new string [] { String.Empty });
		}

		[Test]
		public void StringPref ()
		{
			const string defaultValue = "hello";
			var pref = new Preference<string> ("StringPref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (null);
			// NSUserDefaults does not support null, so we specify that we always write empty
			pref.GetValue ().ShouldBeEmpty ();
			pref.SetValue (String.Empty);
			pref.GetValue ().ShouldBeEmpty ();
			pref.SetValue ("something else");
			pref.GetValue ().ShouldEqual ("something else");
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void BoolPref ()
		{
			const bool defaultValue = true;
			var pref = new Preference<bool> ("BoolPref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (false);
			pref.GetValue ().ShouldEqual (false);
			pref.SetValue (true);
			pref.GetValue ().ShouldEqual (true);
			pref.SetValue (false);
			pref.GetValue ().ShouldEqual (false);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void SBytePref ()
		{
			const sbyte defaultValue = 99;
			var pref = new Preference<sbyte> ("SBytePref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual<sbyte> (0);
			pref.SetValue (SByte.MaxValue);
			pref.GetValue ().ShouldEqual (SByte.MaxValue);
			pref.SetValue (SByte.MinValue);
			pref.GetValue ().ShouldEqual (SByte.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void BytePref ()
		{
			const byte defaultValue = 99;
			var pref = new Preference<byte> ("BytePref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual<byte> (0);
			pref.SetValue (Byte.MaxValue);
			pref.GetValue ().ShouldEqual (Byte.MaxValue);
			pref.SetValue (Byte.MinValue);
			pref.GetValue ().ShouldEqual (Byte.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void Int16Pref ()
		{
			const short defaultValue = 99;
			var pref = new Preference<short> ("Int16Pref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual<short> (0);
			pref.SetValue (Int16.MaxValue);
			pref.GetValue ().ShouldEqual (Int16.MaxValue);
			pref.SetValue (Int16.MinValue);
			pref.GetValue ().ShouldEqual (Int16.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void UInt16Pref ()
		{
			const ushort defaultValue = 99;
			var pref = new Preference<ushort> ("UInt16Pref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual<ushort> (0);
			pref.SetValue (UInt16.MaxValue);
			pref.GetValue ().ShouldEqual (UInt16.MaxValue);
			pref.SetValue (UInt16.MinValue);
			pref.GetValue ().ShouldEqual (UInt16.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}


		[Test]
		public void Int32Pref ()
		{
			const int defaultValue = 99;
			var pref = new Preference<int> ("Int32Pref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual (0);
			pref.SetValue (Int32.MaxValue);
			pref.GetValue ().ShouldEqual (Int32.MaxValue);
			pref.SetValue (Int32.MinValue);
			pref.GetValue ().ShouldEqual (Int32.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void UInt32Pref ()
		{
			const uint defaultValue = 99;
			var pref = new Preference<uint> ("UInt32Pref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual<uint> (0);
			pref.SetValue (UInt32.MaxValue);
			pref.GetValue ().ShouldEqual (UInt32.MaxValue);
			pref.SetValue (UInt32.MinValue);
			pref.GetValue ().ShouldEqual (UInt32.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void Int64Pref ()
		{
			const long defaultValue = 99;
			var pref = new Preference<long> ("Int64Pref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual (0);
			pref.SetValue (Int64.MaxValue);
			pref.GetValue ().ShouldEqual (Int64.MaxValue);
			pref.SetValue (Int64.MinValue);
			pref.GetValue ().ShouldEqual (Int64.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void UInt64Pref ()
		{
			const ulong defaultValue = 99;
			var pref = new Preference<ulong> ("UInt64Pref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual<ulong> (0);
			pref.SetValue (UInt64.MaxValue);
			pref.GetValue ().ShouldEqual (UInt64.MaxValue);
			pref.SetValue (UInt64.MinValue);
			pref.GetValue ().ShouldEqual (UInt64.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void DoublePref ()
		{
			const double defaultValue = 99;
			var pref = new Preference<double> ("DoublePref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual (0.0);
			pref.SetValue (Double.MaxValue);
			pref.GetValue ().ShouldEqual (Double.MaxValue);
			pref.SetValue (Double.MinValue);
			pref.GetValue ().ShouldEqual (Double.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void SinglePref ()
		{
			const float defaultValue = 99;
			var pref = new Preference<float> ("SinglePref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (0);
			pref.GetValue ().ShouldEqual (0.0f);
			pref.SetValue (Single.MaxValue);
			pref.GetValue ().ShouldEqual (Single.MaxValue);
			pref.SetValue (Single.MinValue);
			pref.GetValue ().ShouldEqual (Single.MinValue);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		public enum TestEnum
		{
			A,
			B,
			C
		}

		[Test]
		public void EnumPref ()
		{
			var defaultValue = TestEnum.B;
			var pref = new Preference<TestEnum> ("EnumPref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (TestEnum.A);
			pref.GetValue ().ShouldEqual (TestEnum.A);
			pref.SetValue (TestEnum.B);
			pref.GetValue ().ShouldEqual (TestEnum.B);
			pref.SetValue (TestEnum.C);
			pref.GetValue ().ShouldEqual (TestEnum.C);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Flags]
		public enum TestFlagsEnum
		{
			None = 0,
			A = 1,
			B = 2,
			C = 4,
			D = 8
		}

		[Test]
		public void FlagsEnumPref ()
		{
			var defaultValue = TestFlagsEnum.None;
			var pref = new Preference<TestFlagsEnum> ("FlagsEnumPref", defaultValue);
			pref.GetValue ().ShouldEqual (defaultValue);
			pref.SetValue (TestFlagsEnum.A);
			pref.GetValue ().ShouldEqual (TestFlagsEnum.A);
			pref.SetValue (TestFlagsEnum.A | TestFlagsEnum.D);
			pref.GetValue ().ShouldEqual (TestFlagsEnum.A | TestFlagsEnum.D);
			pref.SetValue (TestFlagsEnum.A | TestFlagsEnum.B | TestFlagsEnum.C);
			pref.GetValue ().ShouldEqual (TestFlagsEnum.A | TestFlagsEnum.B | TestFlagsEnum.C);
			pref.Reset ();
			pref.GetValue ().ShouldEqual (defaultValue);
		}

		[Test]
		public void NamespacedPrefs ()
		{
			var pref1 = new Preference<int> ("1");
			var prefA1 = new Preference<string> ("A.1");
			var prefA2 = new Preference<int> ("A.2");
			var prefB1 = new Preference<double> ("B.1");

			pref1.SetValue (11);
			prefA1.SetValue ("test1");
			prefA2.SetValue (2);
			prefB1.SetValue (1.0);

			pref1.GetValue ().ShouldEqual (11);
			prefA1.GetValue ().ShouldEqual ("test1");
			prefA2.GetValue ().ShouldEqual (2);
			prefB1.GetValue ().ShouldEqual (1.0);

			prefA1.Reset ();
			prefA1.GetValue ().ShouldEqual (prefA1.DefaultValue);
			prefA2.GetValue ().ShouldEqual (2);
			pref1.GetValue ().ShouldEqual (11);

			prefA2.Reset ();
			prefA2.GetValue ().ShouldEqual (prefA2.DefaultValue);

			pref1.Reset ();
			pref1.GetValue ().ShouldEqual (pref1.DefaultValue);
			prefB1.GetValue ().ShouldEqual (1.0);

			prefB1.Reset ();
			prefB1.GetValue ().ShouldEqual (prefB1.DefaultValue);
		}

		[Test]
		public void RemoveAllTest ()
		{
			var pref1 = new Preference<int> ("1");
			var prefA1 = new Preference<string> ("A.1");
			var prefA2 = new Preference<int> ("A.2");
			var prefB1 = new Preference<double> ("B.1");

			pref1.SetValue (11);
			prefA1.SetValue ("test1");
			prefA2.SetValue (2);
			prefB1.SetValue (1.0);

			PreferenceStore.Default.Keys.Count.ShouldEqual (4);

			PreferenceStore.Default.RemoveAll ();

			PreferenceStore.Default.Keys.ShouldBeEmpty ();

			pref1.GetValue ().ShouldEqual (pref1.DefaultValue);
			prefA1.GetValue ().ShouldEqual (prefA1.DefaultValue);
			prefA2.GetValue ().ShouldEqual (prefA2.DefaultValue);
			prefB1.GetValue ().ShouldEqual (prefB1.DefaultValue);
		}

		[Test]
		public void DeleteNonExistantPrefTest ()
		{
			var pref = new Preference<int> ("1");
			pref.Reset (); // This should not throw
		}
	}
}