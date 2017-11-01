//
// YamlMetadataCell.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using CommonMark.Syntax;

namespace Xamarin.Interactive.Workbook.Models
{
	sealed class YamlMetadataCell : Cell
	{
		sealed class YamlBuffer : ICellBuffer
		{
			public string Value { get; set; }
			public int Length => Value == null ? 0 : Value.Length;
		}

		public override ICellBuffer Buffer { get; } = new YamlBuffer ();

		public YamlMetadataCell (Block markdownBlock = null)
		{
			Buffer.Value = markdownBlock?.StringContent.ToString ();
		}

		public override Block ToMarkdownDocumentBlock ()
			=> ToMarkdownDocumentBlock (
				BlockTag.YamlBlock,
				new FencedCodeData { FenceChar = '-' });
	}
}