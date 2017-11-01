//
// Catalog.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.I18N
{
	static class Catalog
	{
		const string TAG = nameof (Catalog);

		public static string GetString (string message, string comment = null)
			=> message;

		public static string GetPluralString (
			string singularMessage,
			string pluralMessage,
	   		int n,
			string comment = null)
			=> n == 1 ? singularMessage : pluralMessage;

		public static string Format (string format, params object [] args)
		{
			#pragma warning disable 0168
			try {
				return String.Format (format, args);
			} catch (FormatException e) {
				#if !CATALOG_API_ONLY
				Logging.Log.Error (TAG, "invalid format string", e);
				#endif
				return format;
			}
			#pragma warning restore 0168
		}

		public static class SharedStrings
		{
			public static readonly string XcodeNotFoundMessage = GetString (
				"Unable to locate a valid Xcode installation. Check the Apple SDK settings " +
				"in Xamarin Studio preferences or ensure that `xcode-select -p` returns a " +
				"valid Xcode installation path.");
		}
	}
}