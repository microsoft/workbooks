//
// JsonWriter.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2007 James Newton-King
// Copyright 2016 Microsoft. All rights reserved.
//
// String writing adapted from Newtonsoft.Json under the MIT license:
//   https://github.com/JamesNK/Newtonsoft.Json

using System;
using System.Globalization;
using System.IO;

namespace Xamarin.Interactive.Serialization
{
	static class JsonWriter
	{
		static readonly IFormatProvider formatProvider = CultureInfo.InvariantCulture;

		public static void WriteJson (this TextWriter writer, bool value)
			=> writer.Write (value ? "true" : "false");

		public static void WriteJson (this TextWriter writer, int value)
			=> writer.Write (value.ToString (formatProvider));

		public static void WriteJson (this TextWriter writer, float value)
			=> writer.Write (value.ToString ("G9", formatProvider));

		public static void WriteJson (this TextWriter writer, double value)
			=> writer.Write (value.ToString ("G17", formatProvider));

		public static void WriteJson (this TextWriter writer, byte [] value)
		{
			if (value == null) {
				writer.Write ("null");
				return;
			}

			writer.Write ('"');
			writer.Write (Convert.ToBase64String (value));
			writer.Write ('"');
		}

		// Adapted from JavaScriptUtils.WriteEscapedJavaScriptString in Newtonsoft.Json
		public static void WriteJson (this TextWriter writer, string value)
		{
			if (value == null) {
				writer.Write ("null");
				return;
			}

			writer.Write ('"');

			for (int i = 0; i < value.Length; i++) {
				var c = value [i];
				string escapedValue;

				switch (c) {
				case '\t':
					escapedValue = @"\t";
					break;
				case '\n':
					escapedValue = @"\n";
					break;
				case '\r':
					escapedValue = @"\r";
					break;
				case '\f':
					escapedValue = @"\f";
					break;
				case '\b':
					escapedValue = @"\b";
					break;
				case '\\':
					escapedValue = @"\\";
					break;
				case '\u0085': // Next Line
					escapedValue = @"\u0085";
					break;
				case '\u2028': // Line Separator
					escapedValue = @"\u2028";
					break;
				case '\u2029': // Paragraph Separator
					escapedValue = @"\u2029";
					break;
				case '"':
					escapedValue = "\\\"";
					break;
				default:
					escapedValue = c <= '\u001f' ? ToCharAsUnicode (c) : null;
					break;
				}

				// FIXME: write the value string directly if we did not need to escape anything

				if (escapedValue != null)
					writer.Write (escapedValue);
				else
					writer.Write (c);
			}

			writer.Write ('"');
		}

		// Adapted from StringUtils in Newtonsoft.Json
		static string ToCharAsUnicode (char c)
			=> new string (new [] {
				'\\', 'u',
				IntToHex ((c >> 12) & '\x000f'),
				IntToHex ((c >> 8) & '\x000f'),
				IntToHex ((c >> 4) & '\x000f'),
				IntToHex (c & '\x000f')
			});

		// Adapted from MathUtils in Newtonsoft.Json
		static char IntToHex (int n)
			=> n <= 9 ? (char)(n + 48) : (char)((n - 10) + 97);
	}
}