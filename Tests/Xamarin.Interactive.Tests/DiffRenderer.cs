//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Xamarin.Interactive.Tests
{
    sealed class DiffRenderer
    {
        public sealed class DiffPieceGroup
        {
            public ChangeType Type { get; }
            public List<DiffPiece> Lines { get; }

            public DiffPieceGroup Previous { get; private set; }
            public DiffPieceGroup Next { get; private set; }

            public DiffPieceGroup (DiffPieceGroup previous, ChangeType type)
            {
                Previous = previous;
                if (previous != null)
                    previous.Next = this;

                Lines = new List<DiffPiece> ();
                Type = type;
            }
        }

        readonly int maxContextLines;
        readonly int maxLineNumberDigits;
        readonly int maxLineWidth;

        public DiffPieceGroup FirstGroup { get; }

        // if we have no groups or only an 'unchanged' group, then there is no diff
        public bool HasDiff => FirstGroup != null &&
            (FirstGroup.Type != ChangeType.Unchanged || FirstGroup.Next != null);

        public DiffRenderer (string oldText, string newText, int maxContextLines = 3, int tabWidth = 8)
        {
            this.maxContextLines = maxContextLines;

            var inlineBuilder = new InlineDiffBuilder (new Differ ());
            var diff = inlineBuilder.BuildDiffModel (oldText, newText);

            int maxLineNumber = 0;
            DiffPieceGroup currentGroup = null;

            foreach (var line in diff.Lines) {
                if (currentGroup == null || currentGroup.Type != line.Type) {
                    currentGroup = new DiffPieceGroup (currentGroup, line.Type);
                    if (FirstGroup == null)
                        FirstGroup = currentGroup;
                }

                if (line.Position != null)
                    maxLineNumber = Math.Max (maxLineNumber, line.Position.Value);

                maxLineWidth = Math.Max (maxLineWidth, GetLineWidth (line.Text, tabWidth));

                currentGroup.Lines.Add (line);
            }

            maxLineNumberDigits = (int)Math.Floor (Math.Log10 (maxLineNumber) + 1);
        }

        static int GetLineWidth (string line, int tabWidth)
        {
            int lineWidth = 0;

            for (int i = 0; i < line.Length; i++) {
                switch (line [i]) {
                case '\t':
                    lineWidth += tabWidth;
                    break;
                case '\n':
                case '\r':
                    break;
                default:
                    lineWidth++;
                    break;
                }
            }

            return lineWidth;
        }

        public void Write (TextWriter writer)
        {
            if (!HasDiff)
                return;

            var group = FirstGroup;
            while (group != null) {
                var lines = group.Lines;

                switch (group.Type) {
                case ChangeType.Inserted:
                case ChangeType.Deleted:
                    foreach (var line in lines)
                        WriteDiffLine (writer, line);
                    break;
                case ChangeType.Unchanged:
                    var nTotal = lines.Count;
                    var nTail = Math.Min (nTotal, maxContextLines);
                    var nHead = Math.Min (nTotal - nTail, maxContextLines);

                    if (group.Previous != null) {
                        for (int i = 0; i < nHead; i++)
                            WriteDiffLine (writer, lines [i]);
                    }

                    if (nTotal > maxContextLines * 2)
                        WriteDiffLine (writer, null);

                    if (group.Next != null) {
                        for (int i = nTotal - nTail; i < nTotal; i++)
                            WriteDiffLine (writer, lines [i]);
                    }

                    break;
                }

                group = group.Next;
            }
        }

        void WriteDiffLine (TextWriter writer, DiffPiece line)
        {
            if (line == null) {
                writer.Write ("@@ ↕ ");
                writer.Write (new string ('-', maxLineWidth + maxLineNumberDigits));
                writer.WriteLine (" ↕ @@");
                return;
            }

            var lineNumber = line.Position == null
                ? string.Empty
                : line.Position.Value.ToString (CultureInfo.InvariantCulture);

            switch (line.Type) {
            case ChangeType.Unchanged:
                writer.Write (' ');
                break;
            case ChangeType.Inserted:
                writer.Write ('+');
                break;
            case ChangeType.Deleted:
                writer.Write ('-');
                break;
            }

            writer.Write (' ');
            writer.Write (lineNumber.PadLeft (maxLineNumberDigits));
            writer.Write (" | ");
            writer.WriteLine (line.Text);
        }
    }
}