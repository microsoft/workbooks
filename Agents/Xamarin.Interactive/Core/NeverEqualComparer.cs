//
// NeverEqualComparer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

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