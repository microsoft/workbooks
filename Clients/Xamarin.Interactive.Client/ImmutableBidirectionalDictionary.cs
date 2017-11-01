//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Xamarin.Interactive
{
    sealed class ImmutableBidirectionalDictionary<TFirst, TSecond>
    {
        public static readonly ImmutableBidirectionalDictionary<TFirst, TSecond> Empty
            = new ImmutableBidirectionalDictionary<TFirst, TSecond> (
                ImmutableDictionary<TFirst, TSecond>.Empty,
                ImmutableDictionary<TSecond, TFirst>.Empty);

        public ImmutableDictionary<TFirst, TSecond> FirstToSecond { get; }
        public ImmutableDictionary<TSecond, TFirst> SecondToFirst { get; }

        ImmutableBidirectionalDictionary (
            ImmutableDictionary<TFirst, TSecond> firstToSecond,
            ImmutableDictionary<TSecond, TFirst> secondToFirst)
        {
            FirstToSecond = firstToSecond;
            SecondToFirst = secondToFirst;
        }

        public ImmutableBidirectionalDictionary<TFirst, TSecond> WithComparers (
            IEqualityComparer<TFirst> firstKeyComparer,
            IEqualityComparer<TSecond> secondKeyComparer)
            => new ImmutableBidirectionalDictionary<TFirst, TSecond> (
                FirstToSecond.WithComparers (firstKeyComparer, secondKeyComparer),
                SecondToFirst.WithComparers (secondKeyComparer, firstKeyComparer));

        public ImmutableBidirectionalDictionary<TFirst, TSecond> Add (TFirst first, TSecond second)
            => new ImmutableBidirectionalDictionary<TFirst, TSecond> (
                FirstToSecond.Add (first, second),
                SecondToFirst.Add (second, first));

        public bool TryGetSecond (TFirst first, out TSecond second)
            => FirstToSecond.TryGetValue (first, out second);

        public bool TryGetFirst (TSecond second, out TFirst first)
            => SecondToFirst.TryGetValue (second, out first);
    }
}