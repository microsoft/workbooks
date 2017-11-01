//
// AssignmentMonitorSyntaxRewriter.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xamarin.Interactive.Compilation.Roslyn
{
	sealed class AssignmentMonitorSyntaxRewriter : CSharpSyntaxRewriter
	{
		readonly SemanticModel semanticModel;

		public AssignmentMonitorSyntaxRewriter (SemanticModel semanticModel)
		{
			this.semanticModel = semanticModel;
		}

		ISymbol GetMonitorableSymbol (SyntaxNode node)
		{
			var symbol = semanticModel.LookupSymbols (node.SpanStart).FirstOrDefault ();
			switch (symbol?.Kind) {
			case SymbolKind.Field:
			case SymbolKind.Local:
				return symbol;
			}
			return null;
		}

		static ArgumentSyntax LiteralArgument (string value)
		{
			return SyntaxFactory.Argument (
				SyntaxFactory.LiteralExpression (
					SyntaxKind.NumericLiteralExpression,
					SyntaxFactory.Literal (value)));
		}

		static ArgumentSyntax LiteralArgument (int value)
		{
			return SyntaxFactory.Argument (
				SyntaxFactory.LiteralExpression (
					SyntaxKind.StringLiteralExpression,
					SyntaxFactory.Literal (value)));
		}

		static ExpressionSyntax InvokeAssignmentMonitor (ISymbol symbol,
			SyntaxNode targetNode,
			ExpressionSyntax sourceNode)
		{
			return SyntaxFactory.InvocationExpression (
				SyntaxFactory.IdentifierName ("__AssignmentMonitor"),
				SyntaxFactory.ArgumentList (
					SyntaxFactory.SeparatedList (new [] {
						SyntaxFactory.Argument (sourceNode),
						LiteralArgument (symbol.Name),
						LiteralArgument (targetNode.Kind ().ToString ()),
						LiteralArgument (targetNode.Span.Start),
						LiteralArgument (targetNode.Span.End)
					})));
		}

		public override SyntaxNode VisitVariableDeclaration (VariableDeclarationSyntax node)
		{
			return node.WithVariables (
				SyntaxFactory.SeparatedList (node
					.Variables
					.Select (v => {
						var symbol = GetMonitorableSymbol (v);
						if (symbol == null)
							return v;

						return v.WithInitializer (
							v.Initializer.WithValue (
								InvokeAssignmentMonitor (
									symbol,
									v,
									v.Initializer.Value)));
					})));
		}

		public override SyntaxNode VisitAssignmentExpression (AssignmentExpressionSyntax node)
		{
			var symbol = GetMonitorableSymbol (node.Left);
			return symbol == null
				? node
				: node.WithRight (InvokeAssignmentMonitor (symbol, node, node.Right));
		}

		public override SyntaxNode VisitPostfixUnaryExpression (PostfixUnaryExpressionSyntax node)
		{
			var symbol = GetMonitorableSymbol (node.Operand);
			return symbol == null
				? node
				: InvokeAssignmentMonitor (symbol, node, node);
		}

		public override SyntaxNode VisitPrefixUnaryExpression (PrefixUnaryExpressionSyntax node)
		{
			var symbol = GetMonitorableSymbol (node.Operand);
			return symbol == null
				? node
				: InvokeAssignmentMonitor (symbol, node, node);
		}
	}
}