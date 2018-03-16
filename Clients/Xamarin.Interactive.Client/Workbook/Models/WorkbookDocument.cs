//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using CommonMark;
using CommonMark.Syntax;

namespace Xamarin.Interactive.Workbook.Models
{
    sealed class WorkbookDocument : IReadOnlyList<Cell>
    {
        static Lazy<CommonMarkSettings> commonMarkSettings = new Lazy<CommonMarkSettings> (() => {
            var settings = CommonMarkSettings.Default.Clone ();
            settings.AdditionalFeatures =
                CommonMarkAdditionalFeatures.YamlBlocks |
                CommonMarkAdditionalFeatures.StrikethroughTilde;
            return settings;
        });

        internal static CommonMarkSettings CommonMarkSettings => commonMarkSettings.Value;

        public Cell FirstCell { get; private set; }
        public Cell LastCell { get; private set; }

        int generation;

        #region Linked List Methods

        void ValidateInsertion (Cell referenceCell, Cell newCell)
        {
            if (referenceCell == null)
                throw new ArgumentNullException (nameof (referenceCell));

            if (newCell == null)
                throw new ArgumentNullException (nameof (newCell));

            if (newCell.Document != null)
                throw new ArgumentException (
                    $"cell already a child of a {nameof (WorkbookDocument)}",
                    nameof (newCell));

            if (referenceCell.Document != this)
                throw new ArgumentException (
                    $"not a child of this {nameof (WorkbookDocument)}",
                    nameof (referenceCell));
        }

        public void InsertCellAfter (Cell referenceCell, Cell newCell)
        {
            ValidateInsertion (referenceCell, newCell);

            generation++;
            Count++;

            newCell.PreviousCell = referenceCell;
            newCell.NextCell = referenceCell.NextCell;

            if (referenceCell.NextCell == null)
                LastCell = newCell;
            else
                referenceCell.NextCell.PreviousCell = newCell;

            referenceCell.NextCell = newCell;

            newCell.Document = this;
        }

        public void InsertCellBefore (Cell referenceCell, Cell newCell)
        {
            ValidateInsertion (referenceCell, newCell);

            generation++;
            Count++;

            newCell.PreviousCell = referenceCell.PreviousCell;
            newCell.NextCell = referenceCell;

            if (referenceCell.PreviousCell == null)
                FirstCell = newCell;
            else
                referenceCell.PreviousCell.NextCell = newCell;

            referenceCell.PreviousCell = newCell;

            newCell.Document = this;
        }

        public void AppendCell (Cell newCell)
        {
            if (newCell == null)
                throw new ArgumentNullException (nameof (newCell));

            if (newCell.Document != null)
                throw new ArgumentException (
                    $"cell already a child of a {nameof (WorkbookDocument)}",
                    nameof (newCell));

            if (LastCell != null) {
                InsertCellAfter (LastCell, newCell);
                LastCell = newCell;
                return;
            }

            generation++;
            Count++;

            FirstCell = newCell;
            LastCell = newCell;
            newCell.PreviousCell = null;
            newCell.NextCell = null;
            newCell.Document = this;
        }

        public void RemoveCell (Cell cell)
        {
            if (cell == null)
                throw new ArgumentNullException (nameof (cell));

            if (cell.Document != this)
                throw new ArgumentException (
                    $"not a child of this {nameof (WorkbookDocument)}",
                    nameof (cell));

            generation++;
            Count--;

            if (cell.PreviousCell == null)
                FirstCell = cell.NextCell;
            else
                cell.PreviousCell.NextCell = cell.NextCell;

            if (cell.NextCell == null)
                LastCell = cell.PreviousCell;
            else
                cell.NextCell.PreviousCell = cell.PreviousCell;

            cell.Document = null;
            cell.PreviousCell = null;
            cell.NextCell = null;
        }

        public TCell GetFirstCell<TCell> () where TCell : Cell
            => FirstCell as TCell ?? FirstCell?.GetNextCell<TCell> ();

        public TCell GetLastCell<TCell> () where TCell : Cell
            => LastCell as TCell ?? LastCell?.GetPreviousCell<TCell> ();

        #endregion

        #region IReadOnlyList

        public int Count { get; private set; }

        public Cell this [int index] {
            get {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException ();

                if (index == 0)
                    return FirstCell;

                if (index == Count - 1)
                    return LastCell;

                // TODO: suboptimal implementation but this is only used by tests
                int i = 0;
                foreach (var cell in this) {
                    if (i++ == index)
                        return cell;
                }

                // should not be reached but we need to throw something
                throw new IndexOutOfRangeException ();
            }
        }

        public IEnumerator<Cell> GetEnumerator ()
        {
            var startGeneration = generation;
            var cell = FirstCell;
            while (cell != null) {
                if (startGeneration != generation)
                    throw new InvalidOperationException ("entries changed while enumerating");

                yield return cell;
                cell = cell.NextCell;
            }
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        #endregion

        #region Parsing

        static bool IsCodeOrMetadataBlock (Block block, out string languageName, out string extraInfo)
        {
            languageName = null;
            extraInfo = null;

            if (block.Tag == BlockTag.YamlBlock)
                return true;

            if (block.Tag != BlockTag.FencedCode)
                return false;

            languageName = block.FencedCodeData?.Info;
            if (languageName == null)
                return false;

            var ofs = languageName.IndexOf (' ');
            if (ofs > 0) {
                extraInfo = languageName.Substring (ofs + 1);
                languageName = languageName.Substring (0, ofs);
            }

            switch (languageName) {
            case "json":
                // legacy JSON manifest
                return block.Top.FirstChild == block;
            case "csharp":
                return true;
            default:
                return false;
            }
        }

        /// <summary>
        /// Reads a CommonMark Markdown document into a Workbook format,
        /// with extension support for YAML blocks.
        /// </summary>
        public void Read (TextReader textReader)
        {
            FirstCell = null;
            LastCell = null;

            var document = CommonMarkConverter.Parse (textReader, CommonMarkSettings);
            var codeBlocks = new List<Block> ();
            var markdownBlocks = new List<Block> ();

            for (var block = document.FirstChild; block != null; block = block.NextSibling) {
                if (IsCodeOrMetadataBlock (block, out var languageName, out var extraInfo)) {
                    AppendCell (markdownBlocks);
                    markdownBlocks.Clear ();

                    block.StringContent.TrimEnd ();

                    if (block.Tag == BlockTag.YamlBlock)
                        AppendCell (new YamlMetadataCell (block));
                    else
                        AppendCell (new CodeCell (block, languageName, extraInfo));

                    codeBlocks.Add (block);
                } else {
                    markdownBlocks.Add (block);
                }
            }

            AppendCell (markdownBlocks);

            foreach (var block in codeBlocks) {
                block.Top = null;
                block.Parent = null;
                block.NextSibling = null;
                #pragma warning disable 0618
                block.Previous = null;
                #pragma warning restore 0618
            }
        }

        void AppendCell (List<Block> blocks)
        {
            if (blocks.Count == 0)
                return;

            #pragma warning disable 0618
            blocks [0].Previous = null;
            #pragma warning restore 0618
            blocks [blocks.Count - 1].NextSibling = null;

            var entryBlock = new Block (BlockTag.Document, 0);
            entryBlock.Top = entryBlock;

            entryBlock.FirstChild = blocks [0];
            entryBlock.LastChild = blocks [blocks.Count - 1];

            foreach (var block in blocks) {
                block.Top = entryBlock;
                block.Parent = entryBlock;
                FixupBlock (entryBlock, block);
            }

            AppendCell (new MarkdownCell (entryBlock));
        }

        void FixupBlock (Block topBlock, Block block)
        {
            for (; block != null; block = block.NextSibling) {
                block.Top = topBlock;

                // FIXME: ProseMirror 0.8 does not support fenced code blocks, so
                // translate them to HTML blocks to avoid a JS crash when opening
                // the document. This is fixed in newer ProseMirror.
                // cf. https://github.com/ProseMirror/prosemirror/issues/408
                if (block.Tag == BlockTag.FencedCode) {
                    var writer = new StringWriter ();
                    new CommonMark.Formatters.HtmlFormatter (writer, null).WriteDocument (block);
                    var html = writer.ToString ();

                    block.Tag = BlockTag.HtmlBlock;
                    block.HtmlBlockType = HtmlBlockType.InterruptingBlockWithEmptyLines;

                    block.StringContent = new StringContent ();
                    block.StringContent.Append (html, 0, html.Length);
                }

                if (block.FirstChild != null)
                    FixupBlock (topBlock, block.FirstChild);
            }
        }

        #endregion

        #region Writing

        public void Write (TextWriter writer)
        {
            var settings = CommonMarkSettings.WithMarkdownFormatter ();
            var wrotePrevious = false;

            foreach (var cell in this) {
                if (!cell.ShouldSerialize)
                    continue;

                var codeCell = cell as CodeCell;
                if (codeCell != null && string.IsNullOrWhiteSpace (codeCell.Buffer.Value))
                    continue;

                if (wrotePrevious) {
                    writer.WriteLine ();
                    writer.WriteLine ();
                }

                CommonMarkConverter.ProcessStage3 (
                    cell.ToMarkdownDocumentBlock (),
                    writer,
                    settings);

                wrotePrevious = true;
            }
        }

        #endregion
    }
}