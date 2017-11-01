//
// MarkdownFormatter.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using CommonMark.Syntax;

namespace CommonMark.Formatters
{
	sealed class MarkdownFormatter
	{
		sealed class MarkdownTextWriter
		{
			public readonly TextWriter TextWriter;
			readonly List<string> linePrefixes = new List<string> ();

			char lastCharWritten;

			int position;
			public int Position => position;

			public MarkdownTextWriter (TextWriter writer)
			{
				if (writer == null)
					throw new ArgumentNullException (nameof (writer));

				TextWriter = writer;
			}

			public void PushLinePrefix (string linePrefix, bool write = true)
			{
				if (write)
					WriteEscaped (linePrefix);
				linePrefixes.Add (linePrefix);
			}

			public void PopLinePrefix ()
				=> linePrefixes.RemoveAt (linePrefixes.Count - 1);

			void RealWrite (char value)
			{
				// cf. spec § 2.3: Insecure characters: For security reasons, the Unicode
				// character U+0000 must be replaced with the REPLACEMENT CHARACTER (U+FFFD).
				TextWriter.Write (value == char.MinValue ? '\ufffd' : value);

				position++;
				lastCharWritten = value;
			}

			void InternalWrite (char value)
			{
				// we're starting a new line, so write out any line prefixes
				if (lastCharWritten == TextWriter.NewLine [TextWriter.NewLine.Length - 1]) {
					for (int i = 0, n = linePrefixes.Count; i < n; i++) {
						var prefix = linePrefixes [i];
						// if we're writing a blank line, ensure the last
						// prefix does not leave trailing white space
						if (i == n - 1 && value == TextWriter.NewLine [0])
							prefix = prefix.TrimEnd ();
						foreach (var c in prefix)
							RealWrite (c);
					}
				}

				RealWrite (value);
			}

			public void WriteLiteral (char c)
			{
				InternalWrite (c);
			}

			public void WriteLiteral (char c, int repeat)
			{
				for (int i = 0; i < repeat; i++)
					InternalWrite (c);
			}

			public void WriteLiteral (string value)
			{
				if (value == null)
					return;

				for (int i = 0; i < value.Length; i++)
					InternalWrite (value [i]);
			}

			public void WriteLineLiteral ()
				=> WriteLiteral (TextWriter.NewLine);

			public void WriteEscaped (char c)
			{
				InternalWrite ('\\');
				InternalWrite (c);
			}

			public void WriteEscaped (string value, params char [] escapeChars)
			{
				if (value == null)
					return;

				for (int i = 0; i < value.Length; i++) {
					var c = value [i];

					for (int j = 0; j < escapeChars.Length; j++) {
						if (escapeChars [j] == c) {
							InternalWrite ('\\');
							break;
						}
					}

					InternalWrite (c);
				}
			}
		}

		readonly MarkdownTextWriter writer;
		readonly MarkdownFormatterSettings settings;

		public MarkdownFormatter (TextWriter writer, MarkdownFormatterSettings settings = null)
		{
			this.writer = new MarkdownTextWriter (writer);
			this.settings = settings ?? MarkdownFormatterSettings.Default;
		}

		public void WriteBlock (Block block)
		{
			while (block != null) {
				WriteSingleBlock (block);

				block = block.NextSibling;
				if (block == null)
					break;

				writer.WriteLineLiteral ();

				var ancestorList = block.Parent;
				while (ancestorList != null) {
					if (ancestorList.Tag == BlockTag.List)
						break;
					ancestorList = ancestorList.Parent;
				}

				if (ancestorList == null || !ancestorList.ListData.IsTight)
					writer.WriteLineLiteral ();
			}
		}

		void WriteSingleBlock (Block block)
		{
			switch (block.Tag) {
			case BlockTag.Document:
				WriteBlock (block.FirstChild);
				break;

			case BlockTag.ReferenceDefinition:
				// FIXME: these are completely broken in CommonMark.NET as it
				// only persists reference definitions on the document (root)
				// node, which means all context is lost regarding _where_ in
				// the document a reference definition exists. The block here
				// is completely empty. Additionally, it upper-cases the keys
				// the reference dictionary, which means they also cannot be
				// round-tripped correctly :(
				//
				// However - we can still write out _mostly_ semantically
				// identical (e.g. functional) markdown since CommonMark.NET's
				// link inlines will have the resolved URLs.
				break;

			case BlockTag.BlockQuote:
				writer.PushLinePrefix ("> ");
				WriteBlock (block.FirstChild);
				writer.PopLinePrefix ();
				break;

			case BlockTag.Paragraph:
				WriteInline (block.InlineContent);
				break;

			case BlockTag.AtxHeading:
				writer.WriteLiteral ('#', block.Heading.Level);
				writer.WriteLiteral (' ');
				WriteInline (block.InlineContent);
				break;

			case BlockTag.SetextHeading:
				var startPosition = writer.Position;
				WriteInline (block.InlineContent);
				var inlineLength = writer.Position - startPosition;
				var underlineLength = (int)block.Heading.SetextUnderlineLength;
				if (underlineLength < 1)
					underlineLength = inlineLength;
				writer.WriteLineLiteral ();
				writer.WriteLiteral (block.Heading.Level == 1 ? '=' : '-', underlineLength);
				break;

			case BlockTag.ThematicBreak:
				var breakChar = settings.ThematicBreakChar;
				var listData = block.Parent?.ListData;
				if (listData != null && listData.BulletChar == breakChar) {
					if (breakChar == '*')
						breakChar = '-';
					else if (breakChar == '-')
						breakChar = '*';
				}
				writer.WriteLiteral (breakChar, settings.ActualThematicBreakWidth);
				break;

			case BlockTag.FencedCode:
			case BlockTag.YamlBlock:
				var content = block.StringContent.ToString ();
				var fenceOffset = block.FencedCodeData.FenceOffset;
				var fenceChar = block.FencedCodeData.FenceChar;
				var fenceSize = Math.Max (3, MaxConsecutiveCharCount (content, fenceChar) + 1);

				writer.WriteLiteral (' ', fenceOffset);
				writer.WriteLiteral (fenceChar, fenceSize);
				if (!string.IsNullOrEmpty (block.FencedCodeData.Info))
					writer.WriteLiteral (block.FencedCodeData.Info);
				writer.WriteLineLiteral ();

				if (fenceOffset > 0)
					writer.PushLinePrefix (new string (' ', block.FencedCodeData.FenceOffset));

				writer.WriteLiteral (content);

				if (fenceOffset > 0)
					writer.PopLinePrefix ();

				writer.WriteLiteral (' ', fenceOffset);
				writer.WriteLiteral (fenceChar, fenceSize);
				break;

			case BlockTag.IndentedCode:
				writer.PushLinePrefix ("    ");
				// FIXME: RemoveTrailingBlankLines appears to be broken.
				// TrimEnd works, but that's not exactly what we want.
				// Workaround is to convert to a string first, which is
				// is a bit of a perf hit.
				//
				// block.StringContent.RemoveTrailingBlankLines ();
				// block.StringContent.WriteTo (writer);
				writer.WriteLiteral (block.StringContent.ToString ().TrimEnd ('\n', '\r'));
				writer.PopLinePrefix ();
				break;

			case BlockTag.HtmlBlock:
				// FIXME: see comment for IndentedCode case
				writer.WriteLiteral (block.StringContent.ToString ().TrimEnd ('\n', '\r'));
				break;

			case BlockTag.List:
				WriteBlock (block.FirstChild);
				break;

			case BlockTag.ListItem:
				// FIXME: we could compute the ordered list information in the
				// BlockTag.List case above and pass via WriteBlock, but I wanted
				// to keep it simple. Could be an ever-so-slight perf improvement.
				var orderedIndex = block.ListData.Start;
				char orderedDelimiter;
				switch (block.ListData.Delimiter) {
				case ListDelimiter.Period:
					orderedDelimiter = '.';
					break;
				case ListDelimiter.Parenthesis:
					orderedDelimiter = ')';
					break;
				default:
					throw new NotImplementedException (
						$"{nameof (ListDelimiter)}.{block.ListData.Delimiter}");
				}

				string marker;
				switch (block.ListData.ListType) {
				case ListType.Bullet:
					marker = block.ListData.BulletChar.ToString ();
					break;
				case ListType.Ordered:
					marker = (orderedIndex++).ToString (CultureInfo.InvariantCulture)
						+ orderedDelimiter;
					break;
				default:
					throw new NotImplementedException (
						$"{nameof (ListType)}.{block.ListData.ListType}");
				}

				writer.WriteLiteral (' ', block.ListData.MarkerOffset);
				writer.WriteLiteral (marker);
				writer.WriteLiteral (' ', block.ListData.Padding - marker.Length);

				var padding = block.ListData.Padding + block.ListData.MarkerOffset;
				if (padding > 0)
					writer.PushLinePrefix (new string (' ', padding), false);

				WriteBlock (block.FirstChild);

				if (padding > 0)
					writer.PopLinePrefix ();
				break;

			default:
				throw new NotImplementedException ($"{block.Tag}");
			}
		}

		void WriteInline (Inline inline, params char [] escapeChars)
		{
			for (; inline != null; inline = inline.NextSibling)
				WriteSingleInline (ref inline, escapeChars);
		}

		void WriteSingleInline (ref Inline inline, char [] escapeChars)
		{
			switch (inline.Tag) {
			case InlineTag.Emphasis:
			case InlineTag.Strong:
				var delimeterChar = inline.Emphasis.DelimiterCharacter;
				if (delimeterChar == char.MinValue)
					delimeterChar = '_';
				var delimeter = new string (
					delimeterChar,
					inline.Tag == InlineTag.Emphasis ? 1 : 2);
				writer.WriteLiteral (delimeter);
				WriteInline (inline.FirstChild);
				writer.WriteLiteral (delimeter);
				break;

			case InlineTag.Strikethrough:
				writer.WriteLiteral ("~~");
				WriteInline (inline.FirstChild);
				writer.WriteLiteral ("~~");
				break;

			case InlineTag.Placeholder:
				writer.WriteLiteral ('[');
				writer.WriteEscaped (inline.TargetUrl);
				writer.WriteLiteral (']');
				break;

			case InlineTag.Link:
			case InlineTag.Image:
				// for writing out autolinks, we use this potentially stripped
				// URL so we can round-trip email address autolinks.
				var targetUrl = inline.TargetUrl;
				if (targetUrl.StartsWith ("mailto:", StringComparison.Ordinal))
					targetUrl = targetUrl.Substring (7);

				// translate links where the target URL and the content are
				// the same (and no title is specified) into <autolinks>
				if (inline.Tag == InlineTag.Link &&
					string.IsNullOrEmpty (inline.LiteralContent) &&
					inline.FirstChild != null &&
					inline.FirstChild.Tag == InlineTag.String &&
					inline.FirstChild.NextSibling == null &&
					inline.FirstChild.LiteralContent == targetUrl) {
					writer.WriteLiteral ('<');
					writer.WriteEscaped (targetUrl);
					writer.WriteLiteral ('>');
					break;
				}

				// the only difference between a link and an image is ^!
				if (inline.Tag == InlineTag.Image)
					writer.WriteLiteral ('!');

				writer.WriteLiteral ('[');
				WriteInline (inline.FirstChild, '[', ']');
				writer.WriteLiteral (']');
				writer.WriteLiteral ('(');
				writer.WriteEscaped (inline.TargetUrl, '(', ')');
				if (!string.IsNullOrEmpty (inline.LiteralContent)) {
					writer.WriteLiteral (" \"");
					writer.WriteEscaped (inline.LiteralContent, '"', '(', ')');
					writer.WriteLiteral ('"');
				}
				writer.WriteLiteral (')');
				break;

			case InlineTag.Code:
				if (string.IsNullOrEmpty (inline.LiteralContent)) {
					writer.WriteLiteral ("` `");
					break;
				}

				var contentLength = inline.LiteralContent.Length;
				var wrapSize = MaxConsecutiveCharCount (inline.LiteralContent, '`') + 1;

				writer.WriteLiteral ('`', wrapSize);

				if (contentLength > 0 && inline.LiteralContent [0] == '`')
					writer.WriteLiteral (' ');

				writer.WriteLiteral (inline.LiteralContent);

				if (contentLength > 0 && inline.LiteralContent [contentLength - 1] == '`')
					writer.WriteLiteral (' ');

				writer.WriteLiteral ('`', wrapSize);
				break;

			case InlineTag.RawHtml:
				writer.WriteLiteral (inline.LiteralContent);
				break;

			case InlineTag.LineBreak:
				writer.WriteLiteral ('\\');
				writer.WriteLineLiteral ();
				break;

			case InlineTag.SoftBreak:
				writer.WriteLineLiteral ();
				var next = inline.NextSibling;
				if (next != null &&
					next.Tag == InlineTag.String &&
					!string.IsNullOrEmpty (next.LiteralContent)) {
					switch (next.LiteralContent [0]) {
					case '#':
					case '-':
					case '>':
					case '*':
					case '=':
						writer.WriteLiteral (' ', 4);
						break;
					}
				}
				break;

			case InlineTag.String:
				var content = inline.LiteralContent;
				if (string.IsNullOrEmpty (content))
					break;

				// CommonMark.NET and the CommonMark JS reference parser appear
				// to yield single-character string inlines for characters that
				// need escaping. Therefore, we only attempt to escape these
				// strings, and not the first character on a string of an arbitray
				// length.
				switch (content) {
				case "*":
				case "_":
				case ">":
				case "-":
				case "#":
				case "\\":
				case "[":
				case "`":
				case "<":
					writer.WriteEscaped (content [0]);
					break;
				case "!":
					if (inline.NextSibling != null &&
						inline.NextSibling.Tag == InlineTag.Link)
						writer.WriteEscaped ('!');
					else
						writer.WriteLiteral ('!');
					break;
				default:
					// match this inline and a next for escaped ordered list
					// syntax: ^\d+[\.\)]$ - if the two sibling inlines match,
					// we want to escape the list delimeter.
					if (inline.NextSibling != null && content.Length < 9 && (
						inline.NextSibling.LiteralContent == "." ||
						inline.NextSibling.LiteralContent == ")")) {
						var allDigits = true;

						for (int i = 0; i < content.Length; i++) {
							if (!char.IsDigit (content [i])) {
								allDigits = false;
								break;
							}
						}

						if (allDigits) {
							writer.WriteLiteral (content);
							// this will skip over the delimeter inline on the
							// next outer loop iteration since we're handling it
							// here (escaping it).
							inline = inline.NextSibling;
							writer.WriteEscaped (inline.LiteralContent [0]);
							break;
						}
					}

					writer.WriteEscaped (content, escapeChars);
					break;
				}
				break;

			default:
				throw new NotImplementedException ($"{inline.Tag}");
			}
		}

		static int MaxConsecutiveCharCount (string str, char c)
		{
			int max = 0;

			for (int i = 0, run = 0; i < str.Length; i++) {
				if (str [i] == c)
					max = Math.Max (max, ++run);
				else
					run = 0;
			}

			return max;
		}
	}
}