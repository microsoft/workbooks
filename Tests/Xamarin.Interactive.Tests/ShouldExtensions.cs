//
// ShouldExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;

using Should;

using NUnit.Framework;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Xamarin.Interactive.Tests
{
	[Flags]
	public enum ShouldEqualOptions
	{
		None = 0x0,

		/// <summary>
		/// Ignores differences between line endings. <code>\r\n</code>
		/// and <code>\n</code> are treated as the same.
		/// </summary>
		IgnoreLineEndings = 0x1,

		/// <summary>
		/// Consider two texts equal as long as there are no text diff
		/// changes. Ignores whitespace, implying <see cref="IgnoreLineEndings"/>.
		/// This option will show an actual diff if there are differences.
		/// </summary>
		LineDiff = 0x2
	}

	public static class ShouldExtensions
	{
		public static T ShouldBeInstanceOf<T> (this object actual)
		{
			Assert.IsInstanceOf<T> (actual);
			return (T)actual;
		}

		public static void ShouldBeInstanceOf (this object actual, Type expected)
		{
			Assert.That (actual, Is.InstanceOf (expected));
		}

		public static T And<T> (this T t, Action<T> and)
		{
			and.ShouldNotBeNull ();
			and (t);
			return t;
		}

		public static void ShouldEqual (
			this NotifyCollectionChangedEventArgs actual,
			NotifyCollectionChangedEventArgs expected)
		{
			actual.Action.ShouldEqual (expected.Action, "Action");
			actual.NewItems.SequenceShouldEqual (expected.NewItems, "NewItems");
			actual.OldItems.SequenceShouldEqual (expected.OldItems, "OldItems");
			actual.NewStartingIndex.ShouldEqual (expected.NewStartingIndex, "NewStartingIndex");
			actual.OldStartingIndex.ShouldEqual (expected.OldStartingIndex, "OldStartingIndex");
		}

		public static void SequenceShouldEqual (
			this IEnumerable actual,
			IEnumerable expected,
			string userMessage = null)
		{
			if (expected == null)
				actual.ShouldBeNull ();
			else
				expected.Cast<object> ().SequenceShouldEqual (actual.Cast<object> (), userMessage);
		}

		public static void SequenceShouldEqual<T> (
			this IEnumerable<T> actual,
			IEnumerable<T> expected,
			string userMessage = null)
		{
			if (expected == null)
				actual.ShouldBeNull ();
			else
				actual.ToArray ().ShouldEqual (expected.ToArray (), userMessage);
		}

		public static string NormalizeLineEndings (this string str)
			=> str?.Replace ("\r\n", "\n");

		public static void ShouldEqual (
			this string actual,
			string expected,
			ShouldEqualOptions options = ShouldEqualOptions.None)
		{
			if (options.HasFlag (ShouldEqualOptions.LineDiff)) {
				AssertLineDiff (actual, expected);
				return;
			}

			if (options.HasFlag (ShouldEqualOptions.IgnoreLineEndings)) {
				actual = actual.NormalizeLineEndings ();
				expected = expected.NormalizeLineEndings ();
			}

			Assert.AreEqual (expected, actual);
		}

		public static void ShouldNotBeReached (this object actual)
		{
			throw new InvalidOperationException ("should not be reached");
		}

		static void AssertLineDiff (string actual, string expected)
		{
			var diffRenderer = new DiffRenderer (expected, actual);
			if (!diffRenderer.HasDiff)
				return;

			var writer = new StringWriter ();

			writer.WriteLine ();

			if (actual.Length == expected.Length)
				writer.WriteLine ("Actual differs from Expected:");
			else
				writer.WriteLine (
					"Actual (length={0}) differs from Expected (length={1}):",
					actual.Length,
					expected.Length);
			writer.WriteLine ();

			diffRenderer.Write (writer);

			throw new AssertionException (writer.ToString ());
		}
	}
}