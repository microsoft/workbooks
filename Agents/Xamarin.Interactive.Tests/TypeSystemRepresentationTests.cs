//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using Xunit;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Representations.Reflection
{
    public sealed class TypeSystemRepresentationTests
    {
        static void AssertCsharp (string expectedRendering, TypeSpec typeSpec)
        {
            var buffer = new StringWriter ();
            var writer = new CSharpWriter (buffer) { WriteLanguageKeywords = true };
            writer.VisitTypeSpec (typeSpec);
            Assert.Equal (expectedRendering, buffer.ToString ());
        }

        static void AssertCsharp (string expectedRoundTrip, string typeSpec)
            => AssertCsharp (expectedRoundTrip, TypeSpec.Parse (typeSpec));

        static void AssertCsharp (string expectedRoundTrip, Type type)
            => AssertCsharp (expectedRoundTrip, TypeSpec.Create (type));

        [Theory]
        [InlineData ("A")]
        [InlineData ("A.B")]
        [InlineData ("Aa.Bb")]
        public void SimpleParse (string expectedRoundTrip)
            => AssertCsharp (expectedRoundTrip, TypeSpec.Parse (expectedRoundTrip));

        [Theory]
        [InlineData ("void", typeof (void))]
        [InlineData ("object", typeof (object))]
        [InlineData ("bool", typeof (bool))]
        [InlineData ("sbyte", typeof (sbyte))]
        [InlineData ("byte", typeof (byte))]
        [InlineData ("short", typeof (short))]
        [InlineData ("ushort", typeof (ushort))]
        [InlineData ("int", typeof (int))]
        [InlineData ("uint", typeof (uint))]
        [InlineData ("long", typeof (long))]
        [InlineData ("ulong", typeof (ulong))]
        [InlineData ("float", typeof (float))]
        [InlineData ("double", typeof (double))]
        [InlineData ("decimal", typeof (decimal))]
        [InlineData ("char", typeof (char))]
        [InlineData ("string", typeof (string))]
        public void SimpleCreate (string expectedRoundTrip, Type type)
            => AssertCsharp (expectedRoundTrip, type);

        [Theory]
        [InlineData ("int*", typeof (int*))]
        [InlineData ("int**", typeof (int**))]
        [InlineData ("int***", typeof (int***))]
        [InlineData ("int****", typeof (int****))]
        [InlineData ("int*****", typeof (int*****))]

        [InlineData ("int[]", typeof (int[]))]
        [InlineData ("int[,]", typeof (int[,]))]
        [InlineData ("int[,,]", typeof (int[,,]))]
        [InlineData ("int[,,,]", typeof (int[,,,]))]
        [InlineData ("int[,,,,]", typeof (int[,,,,]))]

        [InlineData ("int*[]", typeof (int*[]))]
        [InlineData ("int**[,]", typeof (int**[,]))]
        [InlineData ("int***[,,]", typeof (int***[,,]))]
        [InlineData ("int****[,,,]", typeof (int****[,,,]))]
        [InlineData ("int*****[,,,,]", typeof (int*****[,,,,]))]
        public void ModifiedCreate (string expectedRoundTrip, Type type)
            => AssertCsharp (expectedRoundTrip, type);

        [Theory]
        [InlineData ("A.B", "A+B")]
        [InlineData ("A.B.C", "A.B+C")]
        [InlineData ("A.B.C.D", "A.B.C+D")]
        [InlineData ("A.B.C.D.E", "A.B.C+D+E")]
        public void SimpleNestedParse (string expectedRoundTrip, string typeSpec)
            => AssertCsharp (expectedRoundTrip, typeSpec);

        [Theory]
        [InlineData ("A<B>", "A`1[B]")]
        [InlineData ("A<B, C>", "A`2[B,C]")]
        [InlineData ("A<B, C, D>", "A`3[B,C,D]")]
        [InlineData ("A<B, C<D, E>, F>", "A`3[B,C`2[D,E],F]")]
        public void GenericParse (string expectedRoundTrip, string typeSpec)
            => AssertCsharp (expectedRoundTrip, typeSpec);

        [Theory]
        [InlineData (
            "NS0.NS1.A<G1>.B<G2, G3<G4>>.C<G5, G6, G7>[][]",
            "NS0.NS1.A`1+B`2+C`3[G1,G2,G3`1[G4],G5,G6,G7][][], assembly name"
        )]
        public void GenericNestedParse (string expectedRoundTrip, string typeSpec)
            => AssertCsharp (expectedRoundTrip, typeSpec);
    }
}