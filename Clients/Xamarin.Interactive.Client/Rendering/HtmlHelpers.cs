//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace Xamarin.Interactive.Rendering
{
    public static class HtmlHelpers
    {
        public static bool TryHtmlEscape (this char c, out string escaped, bool newlineToBr = false)
        {
            switch (c) {
            case '<':
                escaped = "&lt;";
                return true;
            case '>':
                escaped = "&gt;";
                return true;
            case '"':
                escaped = "&quot;";
                return true;
            case '\n':
                if (newlineToBr) {
                    escaped = "<br />";
                    return true;
                }

                escaped = null;
                return false;
            default:
                escaped = null;
                return false;
            }
        }

        public static string HtmlEscape (this string str, bool newlineToBr = false)
        {
            if (str == null)
                return null;

            var builder = new StringBuilder (str.Length * 2);

            for (int i = 0; i < str.Length; i++) {
                string escaped;
                if (TryHtmlEscape (str [i], out escaped, newlineToBr))
                    builder.Append (escaped);
                else
                    builder.Append (str [i]);
            }

            return builder.ToString ();
        }

        public static void WriteHtmlEscaped (this TextWriter writer, char c, bool newlineToBr = false)
        {
            string escaped;
            if (TryHtmlEscape (c, out escaped, newlineToBr))
                writer.Write (escaped);
            else
                writer.Write (c);
        }

        public static void WriteHtmlEscaped (this TextWriter writer, string str, bool newlineToBr = false)
        {
            if (str == null)
                return;

            for (int i = 0; i < str.Length; i++)
                writer.WriteHtmlEscaped (str [i], newlineToBr);
        }
    }
}