//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xunit;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Tests
{
    public sealed class TypeSpecTests
    {
        static void AssertType (Type type)
        {
            Assert.NotNull (type);
            Assert.Equal (type.ToString (), TypeSpec.Create (type).ToString ());
            Assert.Equal (type.AssemblyQualifiedName, TypeSpec.Create (type, true).ToString ());
        }

        static TypeSpec AssertType (string typeSpecString)
        {
            Assert.NotNull (typeSpecString);
            var typeSpec = TypeSpec.Parse (typeSpecString);
            Assert.Equal (typeSpecString, typeSpecString.ToString ());
            return typeSpec;
        }

        [Theory]
        [InlineData ("A", null, "A")]
        [InlineData ("AB", null, "AB")]
        [InlineData ("ABC", null, "ABC")]
        [InlineData ("A.B", "A", "B")]
        [InlineData ("AB.CD", "AB", "CD")]
        [InlineData ("ABC.DEF", "ABC", "DEF")]
        [InlineData ("ABC.DEF.H", "ABC.DEF", "H")]
        [InlineData ("ABC.DEF.HIJ", "ABC.DEF", "HIJ")]
        public void Simple (string typeSpec, string expectedNamespace, string expectedName)
        {
            var parsedTypeSpec = AssertType (typeSpec);
            Assert.Equal (expectedNamespace, parsedTypeSpec.Name.Namespace);
            Assert.Equal (expectedName, parsedTypeSpec.Name.Name);
        }

        [Theory]
        [InlineData (typeof(int*))]
        [InlineData (typeof(int**))]
        [InlineData (typeof(int***))]
        [InlineData (typeof(int****))]
        [InlineData (typeof(int*****))]
        [InlineData (typeof(int[]))]
        [InlineData (typeof(int[,]))]
        [InlineData (typeof(int[,,]))]
        [InlineData (typeof(int[,,,]))]
        [InlineData (typeof(int[,,,,]))]
        [InlineData (typeof(int*[]))]
        [InlineData (typeof(int**[,]))]
        [InlineData (typeof(int***[,,]))]
        [InlineData (typeof(int****[,,,]))]
        [InlineData (typeof(int*****[,,,,]))]
        public void ModifiedTypes (Type type)
            => AssertType (type);

        // can't be expressed in C#
        [Theory]
        [InlineData ("int&")]
        [InlineData ("int[*]")]
        [InlineData ("int*[*]*")]
        public void ModifiedTypes_InvalidCSharp (string typeSpec)
            => AssertType (typeSpec);

        [Theory]
        [InlineData ("X&")]
        public void ByRefModifier (string typeSpec)
            => Assert.True (TypeSpec.Parse (typeSpec).IsByRef ());


        [Theory]
        [InlineData ("X*", 0)]
        [InlineData ("X**", 0)]
        [InlineData ("X**", 1)]
        public void PointerModifier (string typeSpec, int modifierIndex)
            => Assert.Equal (TypeSpec.Modifier.Pointer, TypeSpec.Parse (typeSpec).Modifiers [modifierIndex]);

        [Theory]
        [InlineData ("X[]", 0, false, 1)]
        [InlineData ("X[*]", 0, true, 1)]
        [InlineData ("X[,]", 0, false, 2)]
        [InlineData ("X[,,]", 0, false, 3)]
        [InlineData ("X[,,,]", 0, false, 4)]
        public void ArrayModifier (string typeSpec, byte modifierIndex, bool isBound, int dimension)
        {
            var modifier = TypeSpec.Parse (typeSpec).Modifiers [modifierIndex];
            if (isBound)
                Assert.Equal (TypeSpec.Modifier.BoundArray, modifier);
            else
                Assert.Equal (dimension, (byte)modifier);
        }

        [Theory]
        [InlineData ("A+B", 1)]
        [InlineData ("A.B+C", 1)]
        [InlineData ("A.B.C+D", 1)]
        [InlineData ("A.B.C+D+E", 2)]
        [InlineData ("A+B+C+D", 3)]
        [InlineData ("A+B+C+D+E", 4)]
        public void SimpleNested (string typeSpec, int nestedNameCount)
            => Assert.Equal (nestedNameCount, AssertType (typeSpec).NestedNames.Count);

        [Theory]
        [InlineData ("A`1[B]", 1)]
        [InlineData ("A`2[B,C]", 2)]
        [InlineData ("A`3[B,C,D]", 3)]
        public void Generic (string typeSpec, int typeArgumentCount)
        {
            var parsedTypeSpec = AssertType (typeSpec);
            Assert.Equal (typeArgumentCount, parsedTypeSpec.Name.TypeArgumentCount);
            Assert.Equal (typeArgumentCount, parsedTypeSpec.TypeArguments.Count);
        }

        [Fact]
        public void GenericNested ()
        {
            const string ts1s = "A`1+B`2+C`3[G1,G2,G3`1[G4],G5,G6,G7]";
            AssertType (ts1s);
            var ts1 = TypeSpec.Parse (ts1s);
            Assert.Equal (6, ts1.TypeArguments.Count);
            Assert.Equal (1, ts1.Name.TypeArgumentCount);
            Assert.Equal (2, ts1.NestedNames.Count);
            Assert.Equal (2, ts1.NestedNames [0].TypeArgumentCount);
            Assert.Equal (3, ts1.NestedNames [1].TypeArgumentCount);
        }

        [Fact]
        public void AssemblyNameQualified ()
        {
            AssertType ("X, A");
            AssertType ("X`1[[Y, A]], A");
            Assert.Equal ("A, B, C", TypeSpec.Parse ("X, A, B, C").AssemblyName);

            var ts1 = TypeSpec.Parse ("W`3[[X, A, B, C],Y,[Z]], D, E, F");
            Assert.Equal ("D, E, F", ts1.AssemblyName);
            Assert.Equal ("A, B, C", ts1.TypeArguments [0].AssemblyName);
            Assert.Null (ts1.TypeArguments [1].AssemblyName);
            Assert.Null (ts1.TypeArguments [2].AssemblyName);
        }

        [Theory]
        [InlineData (typeof(A))]
        [InlineData (typeof(A.B<A>))]
        [InlineData (typeof(A.B<A.B<A>>.C))]
        [InlineData (typeof(A.B<A.B<A>>.C.D<A.B<A.B<A>>, A.B<A.B<A>>.C, string>.E.F.G<A.B<A>, A>.H))]
        [InlineData (typeof(A.B<Dictionary<A.B<int>, A.B<string>.C.D<A.B<A.B<A.B<A>>>, bool, double>>>))]
        public void Complex (Type type)
            => AssertType (type);

        class A {
            public class B<T1> {
                public class C {
                    public class D<T2, T3, T4> {
                        public class E {
                            public class F {
                                public class G<T5, T6> {
                                    public class H {
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        // https://bugzilla.xamarin.com/show_bug.cgi?id=40060
        public void GenericsAndAnonymous ()
        {
            const string spec = "System.Collections.Generic.Dictionary`2[System.Int32,<>__AnonType0`1[System.Int32]]";

            AssertType (spec);

            var type = TypeSpec.Parse (spec);
            type.Name.Namespace.ShouldEqual ("System.Collections.Generic");
            type.Name.Name.ShouldEqual ("Dictionary");
            type.Name.TypeArgumentCount.ShouldEqual (2);
            type.TypeArguments.Count.ShouldEqual (2);
            type.TypeArguments [0].Name.Namespace.ShouldEqual ("System");
            type.TypeArguments [0].Name.Name.ShouldEqual ("Int32");
            type.TypeArguments [1].Name.Namespace.ShouldBeNull ();
            type.TypeArguments [1].Name.Name.ShouldEqual ("<>__AnonType0");
            type.TypeArguments [1].Name.TypeArgumentCount.ShouldEqual (1);
            type.TypeArguments [1].TypeArguments.Count.ShouldEqual (1);
            type.TypeArguments [1].TypeArguments [0].Name.Namespace.ShouldEqual ("System");
            type.TypeArguments [1].TypeArguments [0].Name.Name.ShouldEqual ("Int32");
        }
    }
}