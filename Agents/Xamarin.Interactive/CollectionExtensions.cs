//
// CollectionExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Collections.Specialized;

namespace Xamarin.Interactive
{
	static class CollectionExtensions
	{
		public static void InsertOrUpdate (
			this OrderedDictionary dictionary,
			int index,
			object key,
			object value)
		{
			if (dictionary.Contains (key))
				dictionary [key] = value;
			else
				dictionary.Insert (index, key, value);
		}

		public static bool TryGetValue (
			this OrderedDictionary dictionary,
			object key,
			out object value)
		{
			if (dictionary.Contains (key)) {
				value = dictionary [key];
				return true;
			}

			value = null;
			return false;
		}

		public static bool TryGetValueAs<T> (
			this OrderedDictionary dictionary,
			object key,
			out T value)
		{
			object boxedValue;
			if (!TryGetValue (dictionary, key, out boxedValue) || !(boxedValue is T)) {
				value = default (T);
				return false;
			}

			value = (T)boxedValue;
			return true;
		}
	}
}