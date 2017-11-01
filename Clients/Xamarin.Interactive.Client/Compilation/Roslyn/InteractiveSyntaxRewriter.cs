//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Compilation.Roslyn
{
    sealed class InteractiveSyntaxRewriter : CSharpSyntaxRewriter
    {
        const string TAG = nameof (InteractiveSyntaxRewriter);

        readonly SemanticModel semanticModel;

        public ImmutableList<LoadDirectiveTriviaSyntax> LoadDirectives { get; private set; }
            = ImmutableList<LoadDirectiveTriviaSyntax>.Empty;

        public InteractiveSyntaxRewriter (SemanticModel semanticModel)
            : base (visitIntoStructuredTrivia: true)
        {
            this.semanticModel = semanticModel
                ?? throw new ArgumentNullException (nameof (semanticModel));
        }

        public override SyntaxNode VisitLoadDirectiveTrivia (LoadDirectiveTriviaSyntax node)
        {
            LoadDirectives = LoadDirectives.Add (node);
            return base.VisitLoadDirectiveTrivia (node);
        }

        public override SyntaxNode VisitObjectCreationExpression (ObjectCreationExpressionSyntax node)
        {
            try {
                if (node.ArgumentList != null && node.ArgumentList.Arguments.Count == 0) {
                    var type = semanticModel.GetSymbolInfo (node).Symbol?.ContainingType;
                    if (type?.Name == "HttpClient" &&
                        type.ContainingAssembly.Identity.Name == "System.Net.Http" &&
                        type.ContainingNamespace.ToString () == "System.Net.Http")
                        node = RewriteDefaultHttpClientObjectCreation (node);
                }
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            return base.VisitObjectCreationExpression (node);
        }

        /// <summary>
        /// Rewrites `new HttpClient ()` as
        /// `new HttpClient (InteractiveAgent.CreateDefaultHttpMessageHandler (), true)`
        /// </summary>
        ObjectCreationExpressionSyntax RewriteDefaultHttpClientObjectCreation (
            ObjectCreationExpressionSyntax node)
            => node.WithArgumentList (
                ArgumentList (
                    SyntaxFactory.CastExpression (
                        SyntaxFactory.ParseTypeName ("System.Net.Http.HttpMessageHandler"),
                        SyntaxFactory.InvocationExpression (
                            SyntaxFactory.MemberAccessExpression (
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName (
                                    "InteractiveAgent"),
                                SyntaxFactory.IdentifierName (
                                    "CreateDefaultHttpMessageHandler")))),
                    SyntaxFactory.LiteralExpression (
                        SyntaxKind.TrueLiteralExpression)));

        static ArgumentListSyntax ArgumentList (params ExpressionSyntax [] argumentExpressions)
            => SyntaxFactory.ArgumentList (
                SyntaxFactory.SeparatedList (
                    argumentExpressions.Select (SyntaxFactory.Argument)));

        /// <summary>
        /// Rewrite assignments to Thread.Current(UI)Culture and CultureInfo.Current(UI)Culture
        /// to InteractiveCulture.Current(UI)Culture, working around a bug in Mono:
        /// https://bugzilla.xamarin.com/show_bug.cgi?id=54448
        /// <seealso cref="Repl.ReplExecutionContext.RunAsync"/>,
        /// <seealso cref="InteractiveCulture"/>
        /// </summary>
        public override SyntaxNode VisitAssignmentExpression (AssignmentExpressionSyntax node)
        {
            var memberAccess = node.Left as MemberAccessExpressionSyntax;
            var memberAccessExpr = memberAccess?.Expression as MemberAccessExpressionSyntax;

            if (memberAccess != null && memberAccessExpr != null && (
                IsMemberAccessForSymbolNamed (
                    memberAccess,
                    "System.Threading.Thread.CurrentCulture",
                    "System.Threading.Thread.CurrentUICulture") &&
                IsMemberAccessForSymbolNamed (
                    memberAccessExpr,
                    "System.Threading.Thread.CurrentThread")) ||
                IsMemberAccessForSymbolNamed (
                    memberAccess,
                    "System.Globalization.CultureInfo.CurrentCulture",
                    "System.Globalization.CultureInfo.CurrentUICulture")) {
                memberAccess = SyntaxFactory.MemberAccessExpression (
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseName ("Xamarin.Interactive.InteractiveCulture"),
                    SyntaxFactory.IdentifierName (memberAccess.Name.Identifier));
                node = node.WithLeft (memberAccess);
            }

            return base.VisitAssignmentExpression (node);
        }

        bool IsMemberAccessForSymbolNamed (MemberAccessExpressionSyntax node, params string [] names)
        {
            if (node == null)
                return false;

            var symbol = semanticModel.GetSymbolInfo (node).Symbol?.ToString ();

            if (symbol == null)
                return false;

            for (int i = 0; i < names.Length; i++) {
                if (symbol == names [i])
                    return true;
            }

            return false;
        }
    }
}