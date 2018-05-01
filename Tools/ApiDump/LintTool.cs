//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using ICSharpCode.NRefactory.CSharp;

namespace ApiDump
{
    public sealed class LintTool : IVisitorTool
    {
        public enum Rule
        {
            None,
            UnsealedPublicClass
        }

        public sealed class Issue
        {
            public Rule Rule { get; }
            public AstNode Node { get; }
            public string Description { get; }

            public Issue (Rule rule, AstNode node, string description)
            {
                Rule = rule;
                Node = node;
                Description = description;
            }
        }

        readonly Visitor visitor = new Visitor ();
        IAstVisitor IVisitorTool.Visitor => visitor;

        public IReadOnlyList<Issue> Issues => visitor.issues;

        sealed class Visitor : DepthFirstAstVisitor
        {
            public readonly List<Issue> issues = new List<Issue> ();

            public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
            {
                if (typeDeclaration.ClassType == ClassType.Class &&
                    typeDeclaration.Modifiers.HasFlag (Modifiers.Public) &&
                    !typeDeclaration.Modifiers.HasFlag (Modifiers.Sealed) &&
                    !typeDeclaration.Modifiers.HasFlag (Modifiers.Static) &&
                    !typeDeclaration.Modifiers.HasFlag (Modifiers.Abstract)) {
                    switch (typeDeclaration.Name) {
                    case "EvaluationContextManager":
                    case "EvaluationContextGlobalObject":
                    case "DependencyResolver":
                    case "NativeDependencyResolver":
                    case "InteractiveDependencyResolver":
                    case "ExternalDependency":
                    case "NativeDependency":
                    case "TokenWriter":
                    case "CSharpWriter":
                        goto visitBase;
                    }

                    var clone = (TypeDeclaration)typeDeclaration.Clone ();
                    clone.Members.Clear ();
                    issues.Add (new Issue (
                        Rule.UnsealedPublicClass,
                        typeDeclaration,
                        $"public non-static, non-abstract class should likely be sealed. Fix or ignore in LintTool:\n{clone}"));
                }

            visitBase:
                base.VisitTypeDeclaration (typeDeclaration);
            }
        }
    }
}