//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;

using ICSharpCode.NRefactory.CSharp;

namespace Xamarin.CrossBrowser.Generator
{
    static class Program
    {
        static void Main (string [] args)
        {
            var dir = Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location);

            var binders = new DomBinder [] {
                new JavaScriptCoreDomBinder (),
                new MshtmlDomBinder ()
            };

            foreach (var binder in binders) {
                SyntaxTree syntaxTree;
                using (var reader = new StreamReader (Path.Combine (dir, "DomApi.cs")))
                    syntaxTree = new CSharpParser ().Parse (reader);

                syntaxTree.AcceptVisitor (binder);
                WriteSources (binder, Path.Combine (dir, "..", binder.OutputDirectory));
            }
        }

        static void WriteSources (DomBinder binder, string dir)
        {
            try {
                Directory.Delete (dir, true);
            } catch {
            }

            Directory.CreateDirectory (dir);

            foreach (var tree in binder.SyntaxTrees) {
                using (var writer = new StreamWriter (Path.Combine (dir, tree.FileName))) {
                    writer.WriteLine ("//");
                    writer.WriteLine ("// WARNING - GENERATED CODE - DO NOT EDIT");
                    writer.WriteLine ("//");
                    writer.WriteLine ("// {0}", tree.FileName);
                    writer.WriteLine ("//");
                    writer.WriteLine ("// Author:");
                    writer.WriteLine ("//   Aaron Bockover <abock@xamarin.com>");
                    writer.WriteLine ("//");
                    writer.WriteLine ("// Copyright 2015-2016 Xamarin Inc. All rights reserved.");
                    writer.WriteLine ();
                    writer.Write (tree.ToString ().Trim ());
                }
            }
        }
    }
}