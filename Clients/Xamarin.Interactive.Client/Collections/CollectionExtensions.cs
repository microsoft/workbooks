//
// CollectionExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Xamarin.Interactive.Collections
{
	static class CollectionExtensions
	{
		public static int IndexOf<T> (
			this IReadOnlyList<T> list,
			T item,
			IEqualityComparer<T> equalityComparer)
		{
			for (int i = 0, n = list.Count; i < n; i++) {
				if (equalityComparer.Equals (list [i], item))
					return i;
			}

			return -1;
		}
	}
}