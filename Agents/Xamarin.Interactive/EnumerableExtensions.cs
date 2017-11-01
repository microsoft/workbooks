//
// EnumerableExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive
{
	static class EnumerableExtensions
	{
		public static void ForEach<T> (this IEnumerable<T> enumerable, Action<T> action)
		{
			if (enumerable == null)
				return;

			if (action == null)
				throw new ArgumentNullException (nameof (action));

			foreach (var item in enumerable)
				action (item);
		}

		public static IEnumerable<T> TraverseTree<T> (this T parent, Func<T, IEnumerable<T>> getEnumerableChildren)
		{
			var nodes = new Stack<T> ();
			nodes.Push (parent);

			while (nodes.Count > 0) {
				var node = nodes.Pop ();
				yield return node;
				foreach (var child in getEnumerableChildren (node))
					nodes.Push (child);

			}
		}
	}
}
