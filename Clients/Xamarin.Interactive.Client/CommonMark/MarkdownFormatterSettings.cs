//
// MarkdownFormatterSettings.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace CommonMark.Formatters
{
	public sealed class MarkdownFormatterSettings
	{
		public static MarkdownFormatterSettings Default { get; } = new MarkdownFormatterSettings {
			MaxWidth = 80,
			ThematicBreakChar = '-'
		};

		/// <summary>
		/// The maximum character width per line, provided the line can be broken with
		/// soft breaks. Set to <c>null</c> to disable soft break wrapping.
		/// </summary>
		public int? MaxWidth { get; set; }

		/// <summary>
		/// The width of thematic breaks. If <c>null</c>, thematic breaks will be as
		/// wide as <seealso cref="MaxWidth"/>. If <seealso cref="MaxWidth"/> is also
		/// <c>null</c>, thematic breaks will be 10 characters wide.
		/// </summary>
		public int? ThematicBreakWidth { get; set; }

		internal int ActualThematicBreakWidth => ThematicBreakWidth ?? MaxWidth ?? 10;

		/// <summary>
		/// The character to use for rendering thematic breaks. Must be one of '*',  '-', or '_'.
		/// </summary>
		public char ThematicBreakChar { get; set; }
	}
}