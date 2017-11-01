//
// MarkdownFormatterExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.IO;

using CommonMark.Formatters;
using CommonMark.Syntax;

namespace CommonMark
{
	static class MarkdownFormatterExtensions
	{
		public static string ToMarkdownString (
			this Block block,
			MarkdownFormatterSettings settings = null)
		{
			using (var writer = new StringWriter ()) {
				new MarkdownFormatter (writer, settings).WriteBlock (block);
				return writer.ToString ();
			}
		}

		static void Formatter (Block block, TextWriter writer, CommonMarkSettings settings)
			=> new MarkdownFormatter (writer).WriteBlock (block);

		public static CommonMarkSettings WithMarkdownFormatter (this CommonMarkSettings settings)
		{
			if (settings.OutputFormat == OutputFormat.CustomDelegate &&
				settings.OutputDelegate == Formatter)
				return settings;

			settings = settings.Clone ();
			settings.OutputFormat = OutputFormat.CustomDelegate;
			settings.OutputDelegate = Formatter;

			return settings;
		}
	}
}