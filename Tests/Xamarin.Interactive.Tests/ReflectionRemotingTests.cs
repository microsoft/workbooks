//
// ReflectionRemotingTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.IO;

using NUnit.Framework;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class ReflectionRemotingTests
	{
		static void AssertCsharp (string expectedRendering, TypeSpec typeSpec)
		{
			var buffer = new StringWriter ();
			var writer = new CSharpWriter (buffer) { WriteLanguageKeywords = true };
			writer.VisitTypeSpec (typeSpec);
			Assert.AreEqual (expectedRendering, buffer.ToString ());
		}

		static void AssertCsharp (string expectedRoundTrip, string typeSpec)
		{
			AssertCsharp (expectedRoundTrip, TypeSpec.Parse (typeSpec));
		}

		static void AssertCsharp (string expectedRoundTrip, Type type)
		{
			AssertCsharp (expectedRoundTrip, TypeSpec.Parse (type));
		}

		static void AssertCsharp (string expectedRoundTrip)
		{
			AssertCsharp (expectedRoundTrip, TypeSpec.Parse (expectedRoundTrip));
		}

		[Test]
		public void Simple ()
		{
			AssertCsharp ("A");
			AssertCsharp ("A.B");
			AssertCsharp ("Aa.Bb");

			AssertCsharp ("void", typeof(void));
			AssertCsharp ("object", typeof(object));
			AssertCsharp ("bool", typeof(bool));
			AssertCsharp ("sbyte", typeof(sbyte));
			AssertCsharp ("byte", typeof(byte));
			AssertCsharp ("short", typeof(short));
			AssertCsharp ("ushort", typeof(ushort));
			AssertCsharp ("int", typeof(int));
			AssertCsharp ("uint", typeof(uint));
			AssertCsharp ("long", typeof(long));
			AssertCsharp ("ulong", typeof(ulong));
			AssertCsharp ("float", typeof(float));
			AssertCsharp ("double", typeof(double));
			AssertCsharp ("decimal", typeof(decimal));
			AssertCsharp ("char", typeof(char));
			AssertCsharp ("string", typeof(string));
		}

		[Test]
		public void Modified ()
		{
			AssertCsharp ("int*", typeof(int*));
			AssertCsharp ("int**", typeof(int**));
			AssertCsharp ("int***", typeof(int***));
			AssertCsharp ("int****", typeof(int****));
			AssertCsharp ("int*****", typeof(int*****));

			AssertCsharp ("int[]", typeof(int[]));
			AssertCsharp ("int[,]", typeof(int[,]));
			AssertCsharp ("int[,,]", typeof(int[,,]));
			AssertCsharp ("int[,,,]", typeof(int[,,,]));
			AssertCsharp ("int[,,,,]", typeof(int[,,,,]));

			AssertCsharp ("int*[]", typeof(int*[]));
			AssertCsharp ("int**[,]", typeof(int**[,]));
			AssertCsharp ("int***[,,]", typeof(int***[,,]));
			AssertCsharp ("int****[,,,]", typeof(int****[,,,]));
			AssertCsharp ("int*****[,,,,]", typeof(int*****[,,,,]));
		}

		[Test]
		public void SimpleNested ()
		{
			AssertCsharp ("A.B", "A+B");
			AssertCsharp ("A.B.C", "A.B+C");
			AssertCsharp ("A.B.C.D", "A.B.C+D");
			AssertCsharp ("A.B.C.D.E", "A.B.C+D+E");
		}

		[Test]
		public void Generic ()
		{
			AssertCsharp ("A<B>", "A`1[B]");
			AssertCsharp ("A<B, C>", "A`2[B,C]");
			AssertCsharp ("A<B, C, D>", "A`3[B,C,D]");
			AssertCsharp ("A<B, C<D, E>, F>", "A`3[B,C`2[D,E],F]");
		}

		[Test]
		public void GenericNested ()
		{
			AssertCsharp (
				"NS0.NS1.A<G1>.B<G2, G3<G4>>.C<G5, G6, G7>[][]",
				"NS0.NS1.A`1+B`2+C`3[G1,G2,G3`1[G4],G5,G6,G7][][], assembly name"
			);
		}
	}
}