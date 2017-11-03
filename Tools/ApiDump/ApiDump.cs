//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;

using Mono.Cecil;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;

namespace ApiDump
{
    public sealed class Driver
    {
        readonly AssemblyDefinition assemblyDefinition;
        readonly List<IVisitorTool> visitorTools = new List<IVisitorTool> ();

        public Driver (string fileName)
        {
            var corlibPath = Path.GetDirectoryName (typeof (object).Assembly.Location);

            var resolver = new DefaultAssemblyResolver ();
            resolver.AddSearchDirectory (corlibPath);
            resolver.AddSearchDirectory (Path.Combine (corlibPath, "Facades"));

            assemblyDefinition = AssemblyDefinition.ReadAssembly (
                fileName,
                new ReaderParameters {
                    AssemblyResolver = resolver
                });
        }

        public void AddVisitorTool (IVisitorTool visitorTool)
            => visitorTools.Add (visitorTool);

        public void Write (TextWriter writer)
        {
            var decompiler = new AstBuilder (new DecompilerContext (assemblyDefinition.MainModule));
            decompiler.AddAssembly (assemblyDefinition);

            var formattingPolicy = FormattingOptionsFactory.CreateMono ().Clone ();

            IAstVisitor [] visitors = {
                new PublicApiVisitor (),
                new SortTreeVisitor (),
                new CSharpOutputVisitor (TokenWriter.Create (writer, indentation: "    "), formattingPolicy)
            };

            foreach (var visitor in visitors)
                decompiler.SyntaxTree.AcceptVisitor (visitor);

            foreach (var tool in visitorTools)
                decompiler.SyntaxTree.AcceptVisitor (tool.Visitor);

            writer.Flush ();
        }
    }
}