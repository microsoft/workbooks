//
// ShouldRenderResultOfExpressionVisitor.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Compilation.Roslyn
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    sealed class ShouldRenderResultOfExpressionVisitor : CSharpSyntaxVisitor<bool>
    {
        readonly Compilation compilation;
        bool isAwaited;

        public ShouldRenderResultOfExpressionVisitor (Compilation compilation)
        {
            if (compilation == null)
                throw new ArgumentNullException (nameof (compilation));

            this.compilation = compilation;
        }

        ISymbol GetSymbol (SyntaxNode node)
            => compilation?.GetSemanticModel (node.SyntaxTree)?.GetSymbolInfo (node).Symbol;

        public override bool DefaultVisit (SyntaxNode node)
            => true;

        public override bool VisitAwaitExpression (AwaitExpressionSyntax node)
        {
            isAwaited = true;
            return node.Expression.Accept (this);
        }

        public override bool VisitIdentifierName (IdentifierNameSyntax node)
        {
            if (isAwaited) {
                var symbol = GetSymbol (node) as IFieldSymbol;
                if (symbol != null && symbol.Type.IsNonGenericTaskType ())
                    return false;
            }

            return true;
        }

        public override bool VisitInvocationExpression (InvocationExpressionSyntax node)
        {
            var methodSymbol = GetSymbol (node) as IMethodSymbol;

            if (methodSymbol != null) {
                if (methodSymbol.ReturnsVoid)
                    return false;

                if (isAwaited && methodSymbol.ReturnType.IsNonGenericTaskType ())
                    return false;
            }

            return true;
        }
    }
}