//
// FlowDocumentFormatter.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Windows;
using System.Windows.Documents;

using CommonMark.Syntax;

using CMInline = CommonMark.Syntax.Inline;
using CMBlock = CommonMark.Syntax.Block;

namespace CommonMark.Formatters
{
	static class FlowDocumentFormatter
	{
		struct Settings
		{
			public Func<string, string> UrlResolver;
			public Func<string, string> PlaceholderResolver;
		}

		public static FlowDocument Format (
			CMBlock block,
			Func<string, string> urlResolver = null,
			Func<string, string> placeholderResolver = null)
		{
			var document = new FlowDocument ();
			FormatBlocks (
				new Settings {
					UrlResolver = urlResolver,
					PlaceholderResolver = placeholderResolver,
				},
				block.FirstChild,
				document.Blocks);
			return document;
		}

		static void FormatBlocks (Settings settings, CMBlock cmBlock, BlockCollection wdBlocks)
		{
			for (; cmBlock != null; cmBlock = cmBlock.NextSibling) {
				switch (cmBlock.Tag) {
				case BlockTag.AtxHeading:
				case BlockTag.SetextHeading:
				case BlockTag.Paragraph:
					var wdParagraph = new Paragraph ();
					if (cmBlock.Tag != BlockTag.Paragraph) {
						// FIXME: this should probably be a Section with header
						// level stored in Section.Tag or a custom style so it
						// can be configured in XAML. Out of time.
						wdParagraph.FontWeight = FontWeights.Bold;
						wdParagraph.FontSize = 14;
					}
					FormatInlines (settings, cmBlock.InlineContent, wdParagraph.Inlines);
					wdBlocks.Add (wdParagraph);
					break;
				case BlockTag.List:
					var wdList = new List ();
					for (var cmChildBlock = cmBlock.FirstChild;
						cmChildBlock != null;
						cmChildBlock = cmChildBlock.NextSibling) {
						var wdListItem = new ListItem ();
						FormatBlocks (settings, cmChildBlock.FirstChild, wdListItem.Blocks);
						wdList.ListItems.Add (wdListItem);
					}
					wdBlocks.Add (wdList);
					break;
				default:
					throw new NotImplementedException ($"BlockTag.{cmBlock.Tag}");
				}
			}
		}

		static void FormatInlines (Settings settings, CMInline cmInline, InlineCollection wdInlines)
		{
			for (; cmInline != null; cmInline = cmInline.NextSibling) {
				switch (cmInline.Tag) {
				case InlineTag.Emphasis:
					var wdItalic = new Italic ();
					FormatInlines (settings, cmInline.FirstChild, wdItalic.Inlines);
					wdInlines.Add (wdItalic);
					break;
				case InlineTag.Strong:
					var wdBold = new Bold ();
					FormatInlines (settings, cmInline.FirstChild, wdBold.Inlines);
					wdInlines.Add (wdBold);
					break;
				case InlineTag.Link:
					var wdLink = new Hyperlink ();
					var url = cmInline.TargetUrl;
					if (settings.UrlResolver != null)
						url = settings.UrlResolver (url);
					wdLink.NavigateUri = new Uri (url);
					FormatInlines (settings, cmInline.FirstChild, wdLink.Inlines);
					wdInlines.Add (wdLink);
					break;
				case InlineTag.Placeholder:
					var placeholder = cmInline.TargetUrl;
					if (settings.PlaceholderResolver != null)
						placeholder = settings.PlaceholderResolver (placeholder);
					wdInlines.Add (new Run (placeholder));
					break;
				case InlineTag.SoftBreak:
					wdInlines.Add (new Run (" "));
					break;
				case InlineTag.LineBreak:
					wdInlines.Add (new LineBreak ());
					break;
				case InlineTag.String:
					wdInlines.Add (new Run (cmInline.LiteralContent));
					break;
				default:
					throw new NotImplementedException ($"InlineTag.{cmInline.Tag}");
				}
			}
		}
	}
}