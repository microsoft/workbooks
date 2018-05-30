// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Xunit;

namespace Xamarin.Interactive.NuGet
{
    public class PackageReferenceListTests
    {
        [Fact]
        public void AddAndReplaceSingle ()
        {
            var list = new TestPackageReferenceList ();

            Assert.True (list.AddOrUpdate (("a", "1.0")), "#1");
            Assert.False (list.AddOrUpdate (("a", "1.0")), "#2");
            Assert.Collection (list, item => Assert.Equal (("a", "1.0"), item));

            Assert.True (list.AddOrUpdate (("a", "2.0")), "#3");
            Assert.False (list.AddOrUpdate (("a", "2.0")), "#4");
            Assert.Collection (list, item => Assert.Equal (("a", "2.0"), item));
        }

        [Fact]
        public void RemoveSingle ()
        {
            var list = new TestPackageReferenceList ();
            list.AddOrUpdate (("a", "1.0"));
            Assert.Collection (list, item => Assert.Equal (("a", "1.0"), item));
            Assert.True (list.Remove (("a", "2.0")), "#1");
            Assert.Empty (list);
        }

        [Fact]
        public void Clear ()
        {
            var list = new TestPackageReferenceList ();
            Assert.Empty (list);
            list.AddOrUpdate (("a", "1.0"));
            list.AddOrUpdate (("a", "2.0"));
            Assert.Collection (list, item => Assert.Equal (("a", "2.0"), item));
            list.Clear ();
            Assert.Empty (list);
        }

        [Fact]
        public void AddAndUpdateMany ()
        {
            var list = new TestPackageReferenceList ();
            list.AddOrUpdate (new InteractivePackageDescription [] {
                ("a", "1.0"),
                ("b", "1.0"),
                ("c", "1.0"),
                ("a", "2.0"),
                ("b", "2.0"),
                ("c", "2.0")
            });

            Assert.Collection (
                list,
                a => Assert.Equal (("a", "2.0"), a),
                b => Assert.Equal (("b", "2.0"), b),
                c => Assert.Equal (("c", "2.0"), c));

            Assert.True (
                list.AddOrUpdate (new InteractivePackageDescription [] {
                    ("d", "1.0"),
                    ("a", "3.0"),
                    ("d", "2.0"),
                    ("e", "1.0")
                }),
                "#1");

            Assert.Collection (
                list,
                a => Assert.Equal (("a", "3.0"), a),
                b => Assert.Equal (("b", "2.0"), b),
                c => Assert.Equal (("c", "2.0"), c),
                d => Assert.Equal (("d", "2.0"), d),
                e => Assert.Equal (("e", "1.0"), e));
        }

        [Fact]
        public void RemoveMany ()
        {
            var list = new TestPackageReferenceList ();
            list.AddOrUpdate (new InteractivePackageDescription [] {
                ("a", "1.0"),
                ("b", "1.0"),
                ("c", "1.0"),
                ("d", "1.0"),
                ("e", "1.0")
            });

            Assert.True (
                list.Remove (new InteractivePackageDescription [] {
                    ("a", null),
                    ("c", null),
                    ("d", "2.0")
                }), "#1");

            Assert.Collection (
                list,
                b => Assert.Equal (("b", "1.0"), b),
                e => Assert.Equal (("e", "1.0"), e));

            Assert.False (
                list.Remove (new InteractivePackageDescription [] {
                    ("a", null),
                    ("c", null),
                    ("d", "2.0")
                }), "#2");

            Assert.True (
                list.Remove (new InteractivePackageDescription [] {
                    ("e", null),
                    ("b", null)
                }), "#3");

            Assert.Empty (list);
        }

        [Fact]
        public void ReplaceAllWith ()
        {
            var list = new TestPackageReferenceList ();
            Assert.True (
                list.ReplaceAllWith (new InteractivePackageDescription [] {
                    ("a", "1.0"),
                    ("b", "1.0"),
                    ("c", "1.0"),
                    ("d", "1.0"),
                    ("e", "1.0")
                }),
                "#1");
            Assert.Collection (
                list,
                a => Assert.Equal (("a", "1.0"), a),
                b => Assert.Equal (("b", "1.0"), b),
                c => Assert.Equal (("c", "1.0"), c),
                d => Assert.Equal (("d", "1.0"), d),
                e => Assert.Equal (("e", "1.0"), e));

            Assert.False (
                list.ReplaceAllWith (new InteractivePackageDescription [] {
                    ("a", "1.0"),
                    ("b", "1.0"),
                    ("c", "1.0"),
                    ("d", "1.0"),
                    ("e", "1.0")
                }),
                "#2");
            Assert.Collection (
                list,
                a => Assert.Equal (("a", "1.0"), a),
                b => Assert.Equal (("b", "1.0"), b),
                c => Assert.Equal (("c", "1.0"), c),
                d => Assert.Equal (("d", "1.0"), d),
                e => Assert.Equal (("e", "1.0"), e));

            Assert.True (
                list.ReplaceAllWith (new InteractivePackageDescription [] {
                    ("a", "1.0"),
                    ("b", "2.0"),
                    ("c", "1.0"),
                    ("d", "1.0"),
                    ("e", "1.0")
                }),
                "#3");
            Assert.Collection (
                list,
                a => Assert.Equal (("a", "1.0"), a),
                b => Assert.Equal (("b", "2.0"), b),
                c => Assert.Equal (("c", "1.0"), c),
                d => Assert.Equal (("d", "1.0"), d),
                e => Assert.Equal (("e", "1.0"), e));

            Assert.False (
                list.ReplaceAllWith (new InteractivePackageDescription [] {
                    ("a", "1.0"),
                    ("b", "2.0"),
                    ("c", "1.0"),
                    ("d", "1.0"),
                    ("e", "1.0")
                }),
                "#4");
            Assert.Collection (
                list,
                a => Assert.Equal (("a", "1.0"), a),
                b => Assert.Equal (("b", "2.0"), b),
                c => Assert.Equal (("c", "1.0"), c),
                d => Assert.Equal (("d", "1.0"), d),
                e => Assert.Equal (("e", "1.0"), e));

            Assert.True (
                list.ReplaceAllWith (new InteractivePackageDescription [] {
                    ("a", "1.0"),
                    ("b", "2.0"),
                    ("c", "1.0"),
                }),
                "#5");
            Assert.Collection (
                list,
                a => Assert.Equal (("a", "1.0"), a),
                b => Assert.Equal (("b", "2.0"), b),
                c => Assert.Equal (("c", "1.0"), c));

            Assert.True (
                list.ReplaceAllWith (new InteractivePackageDescription [] {
                    ("a", "3.0"),
                    ("b", "3.0"),
                    ("c", "3.0"),
                    ("d", "3.0"),
                    ("e", "3.0")
                }),
                "#6");
            Assert.Collection (
                list,
                a => Assert.Equal (("a", "3.0"), a),
                b => Assert.Equal (("b", "3.0"), b),
                c => Assert.Equal (("c", "3.0"), c),
                d => Assert.Equal (("d", "3.0"), d),
                e => Assert.Equal (("e", "3.0"), e));

            Assert.True (
                list.ReplaceAllWith (new InteractivePackageDescription [] {
                    ("a", "3.0"),
                    ("b", "3.0"),
                    ("c", "4.0"),
                    ("d", "3.0"),
                    ("e", "3.0")
                }),
                "#7");
            Assert.Collection (
                list,
                a => Assert.Equal (("a", "3.0"), a),
                b => Assert.Equal (("b", "3.0"), b),
                c => Assert.Equal (("c", "4.0"), c),
                d => Assert.Equal (("d", "3.0"), d),
                e => Assert.Equal (("e", "3.0"), e));

            Assert.True (
                list.ReplaceAllWith (new InteractivePackageDescription [0]),
                "#8");
            Assert.Empty (list);
        }

        [Fact]
        public void InsertionOrder ()
        {
            var list = new TestPackageReferenceList ();
            list.AddOrUpdate (new InteractivePackageDescription [] {
                ("z", null),
                ("x", null),
                ("a", null),
                ("y", null)
            });

            Assert.Collection (
                list,
                z => Assert.Equal (("z", null), z),
                x => Assert.Equal (("x", null), x),
                a => Assert.Equal (("a", null), a),
                y => Assert.Equal (("y", null), y));
        }

        [Fact]
        public void TryGetValue ()
        {
            var list = PackageReferenceList
                .Empty
                .AddOrUpdate (("a", "2.0"));

            Assert.True (list.TryGetValue ("a", out var a));
            Assert.Equal (("a", "2.0"), a);

            Assert.True (list.TryGetValue ("A", out a));
            Assert.Equal (("a", "2.0"), a);

            Assert.False (list.TryGetValue ("b", out _));
        }

        /// <summary>
        /// A "mutable" wrapper around the immutable PackageReferenceList to make for easier testing.
        /// Hint: I might have written all the tests when PackageReferenceList was mutable... -abock
        /// </summary>
        sealed class TestPackageReferenceList : IReadOnlyList<InteractivePackageDescription>
        {
            PackageReferenceList list = PackageReferenceList.Empty;

            public int Count => list.Count;
            public InteractivePackageDescription this [int index] => list [index];

            bool Update (PackageReferenceList updatedList)
            {
                if (list != updatedList) {
                    list = updatedList;
                    return true;
                }

                return false;
            }

            public bool AddOrUpdate (InteractivePackageDescription package)
                => Update (list.AddOrUpdate (package));

            public bool AddOrUpdate (IEnumerable<InteractivePackageDescription> packages)
                => Update (list.AddOrUpdate (packages));

            public bool Remove (InteractivePackageDescription package)
                => Update (list.Remove (package));

            public bool Remove (IEnumerable<InteractivePackageDescription> packages)
                => Update (list.Remove (packages));

            public bool ReplaceAllWith (IEnumerable<InteractivePackageDescription> packages)
                => Update (list.ReplaceAllWith (packages));

            public bool Clear ()
                => Update (list.Clear ());

            public IEnumerator<InteractivePackageDescription> GetEnumerator ()
                => list.GetEnumerator ();

            IEnumerator IEnumerable.GetEnumerator ()
                => GetEnumerator ();
        }
    }
}