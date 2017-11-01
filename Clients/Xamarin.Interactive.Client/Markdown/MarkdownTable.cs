//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Xamarin.Interactive.Markdown
{
    /// <summary>
    /// Build and render a nicely formatted markdown (GFM) table.
    /// </summary>
    class MarkdownTable : IEnumerable<string []>
    {
        readonly string title;
        readonly List<string []> rows = new List<string []> ();

        public MarkdownTable (string title, params string [] columnHeaders)
        {
            this.title = title ?? throw new ArgumentNullException (nameof (title));

            if (columnHeaders.Length == 0)
                throw new ArgumentOutOfRangeException (
                    nameof (columnHeaders),
                    "must have at least one column");

            rows.Add (columnHeaders);

            var separators = new string [columnHeaders.Length];
            for (int i = 0; i < separators.Length; i++)
                separators [i] = ":";

            rows.Add (separators);
        }

        public void Add (params string [] columns)
        {
            if (columns.Length != rows [0].Length)
                throw new ArgumentOutOfRangeException (
                    nameof (columns),
                    $"number of columns ({columns.Length}) does not match " +
                    $"number of column headers ({rows [0].Length})");

            rows.Add (columns);
        }

        public void Render (TextWriter writer)
        {
            writer.WriteLine ($"#### {title}");

            var widths = new int [rows [0].Length];

            foreach (var row in rows) {
                for (int ci = 0; ci < row.Length; ci++)
                    widths [ci] = Math.Max (widths [ci], row [ci].Length);
            }

            for (int ri = 0; ri < rows.Count; ri++) {
                for (int ci = 0; ci < rows [ri].Length; ci++) {
                    var text = rows [ri][ci];
                    var pad = ri == 1 ? '-' : ' ';
                    writer.Write ($"| {text.PadRight (widths [ci], pad)} ");
                }
                writer.WriteLine ("|");
            }
        }

        public IEnumerator<string []> GetEnumerator ()
            => rows.GetEnumerator ();

        IEnumerator IEnumerable.GetEnumerator ()
             => GetEnumerator ();
    }
}