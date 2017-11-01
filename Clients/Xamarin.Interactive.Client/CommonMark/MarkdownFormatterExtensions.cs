//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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