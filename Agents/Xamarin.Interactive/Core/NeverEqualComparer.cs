//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Xamarin.Interactive.Core
{
    class NeverEqualComparer<T> : IEqualityComparer<T>
    {
        public static readonly NeverEqualComparer<T> Default = new NeverEqualComparer<T> ();
        public bool Equals (T x, T y) => false;
        public int GetHashCode (T obj) => 0;
    }
}