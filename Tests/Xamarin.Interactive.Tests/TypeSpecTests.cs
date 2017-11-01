//
// TypeSpecTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public class TypeSpecTests
    {
        static void AssertType (Type type)
        {
            Assert.IsNotNull (type);
            Assert.AreEqual (type.ToString (), TypeSpec.Parse (type).ToString ());
            Assert.AreEqual (type.AssemblyQualifiedName, TypeSpec.Parse (type, true).ToString ());
        }

        static void AssertType (string typeSpec)
        {
            Assert.IsNotNull (typeSpec);
            Assert.AreEqual (typeSpec, TypeSpec.Parse (typeSpec).ToString ());
        }

        [Test]
        public void Simple ()
        {
            AssertType ("A");
            AssertType ("AB");
            AssertType ("ABC");
            AssertType ("A.B");
            AssertType ("AB.CD");
            AssertType ("ABC.DEF");
            Assert.AreEqual ("Aa", TypeSpec.Parse ("Aa.Bb").Name.Namespace);
            Assert.AreEqual ("Bb", TypeSpec.Parse ("Aa.Bb").Name.Name);
        }

        [Test]
        public void Modified ()
        {
            AssertType (typeof(int*));
            AssertType (typeof(int**));
            AssertType (typeof(int***));
            AssertType (typeof(int****));
            AssertType (typeof(int*****));

            AssertType (typeof(int[]));
            AssertType (typeof(int[,]));
            AssertType (typeof(int[,,]));
            AssertType (typeof(int[,,,]));
            AssertType (typeof(int[,,,,]));

            AssertType (typeof(int*[]));
            AssertType (typeof(int**[,]));
            AssertType (typeof(int***[,,]));
            AssertType (typeof(int****[,,,]));
            AssertType (typeof(int*****[,,,,]));

            // can't be expressed in C#
            AssertType ("int&");
            AssertType ("int[*]");
            AssertType ("int*[*]*");

            Assert.IsTrue (TypeSpec.Parse ("X&").IsByRef);
            Assert.IsInstanceOf<TypeSpec.PointerModifier> (TypeSpec.Parse ("X*").Modifiers [0]);
            Assert.IsInstanceOf<TypeSpec.PointerModifier> (TypeSpec.Parse ("X**").Modifiers [0]);
            Assert.IsInstanceOf<TypeSpec.PointerModifier> (TypeSpec.Parse ("X**").Modifiers [1]);
            Assert.IsInstanceOf<TypeSpec.ArrayModifier> (TypeSpec.Parse ("X[]").Modifiers [0]);
            Assert.IsFalse (((TypeSpec.ArrayModifier)TypeSpec.Parse ("X[]").Modifiers [0]).IsBound);
            Assert.IsTrue (((TypeSpec.ArrayModifier)TypeSpec.Parse ("X[*]").Modifiers [0]).IsBound);
            Assert.AreEqual (1, ((TypeSpec.ArrayModifier)TypeSpec.Parse ("X[]").Modifiers [0]).Dimension);
            Assert.AreEqual (2, ((TypeSpec.ArrayModifier)TypeSpec.Parse ("X[,]").Modifiers [0]).Dimension);
            Assert.AreEqual (3, ((TypeSpec.ArrayModifier)TypeSpec.Parse ("X[,,]").Modifiers [0]).Dimension);
        }

        [Test]
        public void SimpleNested ()
        {
            AssertType ("A+B");
            AssertType ("A.B+C");
            AssertType ("A.B.C+D");
            AssertType ("A.B.C+D+E");
            Assert.AreEqual (3, TypeSpec.Parse ("A+B+C+D").NestedNames.Count);
        }

        [Test]
        public void Generic ()
        {
            AssertType ("A`1[B]");
            Assert.AreEqual (2, TypeSpec.Parse ("A`2[B,C]").Name.TypeArgumentCount);
            Assert.AreEqual (2, TypeSpec.Parse ("A`2[B,C]").TypeArguments.Count);
        }

        [Test]
        public void GenericNested ()
        {
            const string ts1s = "A`1+B`2+C`3[G1,G2,G3`1[G4],G5,G6,G7]";
            AssertType (ts1s);
            var ts1 = TypeSpec.Parse (ts1s);
            Assert.AreEqual (6, ts1.TypeArguments.Count);
            Assert.AreEqual (1, ts1.Name.TypeArgumentCount);
            Assert.AreEqual (2, ts1.NestedNames.Count);
            Assert.AreEqual (2, ts1.NestedNames [0].TypeArgumentCount);
            Assert.AreEqual (3, ts1.NestedNames [1].TypeArgumentCount);
        }

        [Test]
        public void AssemblyNameQualified ()
        {
            AssertType ("X, A");
            AssertType ("X`1[[Y, A]], A");
            Assert.AreEqual ("A, B, C", TypeSpec.Parse ("X, A, B, C").AssemblyName);

            var ts1 = TypeSpec.Parse ("W`3[[X, A, B, C],Y,[Z]], D, E, F");
            Assert.AreEqual ("D, E, F", ts1.AssemblyName);
            Assert.AreEqual ("A, B, C", ts1.TypeArguments [0].AssemblyName);
            Assert.IsNull (ts1.TypeArguments [1].AssemblyName);
            Assert.IsNull (ts1.TypeArguments [2].AssemblyName);
        }

        [Test]
        public void Complex ()
        {
            AssertType (typeof(A));
            AssertType (typeof(A.B<A>));
            AssertType (typeof(A.B<A.B<A>>.C));
            AssertType (typeof(A.B<A.B<A>>.C.D<A.B<A.B<A>>, A.B<A.B<A>>.C, string>.E.F.G<A.B<A>, A>.H));
            AssertType (typeof(A.B<Dictionary<A.B<int>, A.B<string>.C.D<A.B<A.B<A.B<A>>>, bool, double>>>));
        }

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

        [Test (Description = "bxc#40060")]
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