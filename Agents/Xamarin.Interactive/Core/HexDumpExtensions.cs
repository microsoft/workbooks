//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Xamarin.Interactive.Core
{
    static class HexDumpExtensions
    {
        public static void HexDump (this byte [] buffer, string label = null)
            => HexDump (buffer, Console.Out, label, 0, buffer.Length);

        public static void HexDump (
            this byte [] buffer,
            TextWriter output,
            string label,
            int offset,
            int count,
            int maxWidth = 16)
        {
            var labelLength = 0;
            if (label != null) {
                output.Write (label);
                labelLength = label.Length;
            }

            var remaining = count;

            while (remaining > 0) {
                if (remaining < count && labelLength > 0)
                    output.Write (String.Empty.PadRight (labelLength));

                var width = Math.Min (remaining, maxWidth);

                for (var i = 0; i < maxWidth; i++) {
                    if (i < width)
                        output.Write ("{0:X2} ", buffer [offset + i]);
                    else
                        output.Write ("   ");

                    if (i + 1 == maxWidth / 2)
                        output.Write (' ');
                }

                output.Write ("  |");

                for (var i = 0; i < maxWidth; i++) {
                    var c = ' ';
                    if (i < width) {
                        c = (char)buffer [offset + i];
                        if (!Char.IsLetterOrDigit (c) &&
                            !Char.IsPunctuation (c) && c != ' ')
                            c = '.';
                    }

                    output.Write (c);
                }

                output.WriteLine ("|");

                offset += width;
                remaining -= width;
            }

            output.Flush ();
        }
    }
}