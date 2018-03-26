//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn
{
    static class SyntaxExtensions
    {
        public static void Dump (this SyntaxNodeOrToken nodeOrToken, TextWriter writer = null, int depth = 0)
        {
            const string indentString = "  ";

            if (nodeOrToken.IsMissing)
                return;

            writer = writer ?? Console.Out;

            for (int i = 0; i < depth; i++)
                writer.Write (indentString);

            if (nodeOrToken.IsNode) {
                var node = nodeOrToken.AsNode ();
                writer.WriteLine ("{0}: {1}", node.GetType ().Name, node);
                foreach (var child in node.ChildNodesAndTokens ())
                    Dump (child, writer, depth + 1);
            } else {
                var token = nodeOrToken.AsToken ();
                writer.WriteLine ("{0}: {1}", token.Kind (), token);
            }
        }

        public static void Dump (this SyntaxToken token, TextWriter writer = null, int depth = 0)
        {
            Dump ((SyntaxNodeOrToken)token, writer, depth);
        }

        public static void Dump (this SyntaxNode node, TextWriter writer = null, int depth = 0)
        {
            Dump ((SyntaxNodeOrToken)node, writer, depth);
        }

        public static string DumpToString (this SyntaxToken token, int depth = 0)
        {
            return DumpToString ((SyntaxNodeOrToken)token, depth);
        }

        public static string DumpToString (this SyntaxNode node, int depth = 0)
        {
            return DumpToString ((SyntaxNodeOrToken)node, depth);
        }

        public static string DumpToString (this SyntaxNodeOrToken nodeOrToken, int depth = 0)
        {
            var writer = new StringWriter ();
            Dump (nodeOrToken, writer, depth);
            return writer.ToString ();
        }
    }
}