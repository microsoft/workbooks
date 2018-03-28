// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Xamarin.Interactive.CodeAnalysis
{
    static class CollectionExtensions
    {
        public static int FindIndex<T> (this IReadOnlyList<T> list, Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException (nameof (match));

            switch (list) {
            case ImmutableList<T> immutableList:
                return immutableList.FindIndex (match);
            case List<T> mutableList:
                return mutableList.FindIndex (match);
            case T [] array:
                return array.FindIndex (match);
            }

            for (int i = 0, n = list.Count; i < n; i++) {
                if (match (list [i]))
                    return i;
            }

            return -1;
        }

        public static int IndexOf<T> (this IReadOnlyList<T> list, T item)
        {
            switch (list) {
            case IImmutableList<T> immutableList:
                return immutableList.IndexOf (item);
            case List<T> mutableList:
                return mutableList.IndexOf (item);
            case T [] array:
                return array.IndexOf (item);
            }

            for (int i = 0, n = list.Count; i < n; i++) {
                if (EqualityComparer<T>.Default.Equals (list [i], item))
                    return i;
            }

            return -1;
        }
    }
}