// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Xunit
{
    static class Should
    {
        public static void ShouldEqual<T> (this T actual, T expected)
            => Assert.Equal<T> (expected, actual);

        public static void ShouldEqual<T> (this T actual, T expected, IEqualityComparer<T> comparer)
            => Assert.Equal<T> (expected, actual, comparer);

        public static void ShouldEqual (this int actual, int expected)
            => Assert.Equal (expected, actual);

        public static void ShouldEqual (this double actual, double expected, int precision)
            => Assert.Equal (expected, actual, precision);

        public static void ShouldEqual (this string actual, string expected)
            => Assert.Equal (expected, actual);

        public static void ShouldEqual (
            this string actual,
            string expected,
            bool ignoreCase = false,
            bool ignoreLineEndingDifferences = false,
            bool ignoreWhiteSpaceDifferences = false)
            => Assert.Equal (
                expected,
                actual,
                ignoreCase,
                ignoreLineEndingDifferences,
                ignoreWhiteSpaceDifferences);

        public static void ShouldEqual<T> (this IEnumerable<T> actual, IEnumerable<T> expected)
            => Assert.Equal (expected, actual);

        public static void ShouldEqual<T> (
            this IEnumerable<T> actual,
            IEnumerable<T> expected,
            IEqualityComparer<T> comparer)
            => Assert.Equal (expected, actual, comparer);

        public static void ShouldBeNull (this object @object)
            => Assert.Null (@object);

        public static void ShouldNotBeNull (this object @object)
            => Assert.NotNull (@object);

        public static T ShouldBeInstanceOf<T> (this object @object)
            => Assert.IsType<T> (@object);

        public static void ShouldNotBeInstanceOf<T> (this object @object)
            => Assert.IsNotType<T> (@object);
    }
}