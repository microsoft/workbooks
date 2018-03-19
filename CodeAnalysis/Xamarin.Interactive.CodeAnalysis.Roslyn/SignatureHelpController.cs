//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.RoslynInternals;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn
{
    sealed class SignatureHelpController
    {
        const string TAG = nameof (SignatureHelpController);

        readonly RoslynCompilationWorkspace compilationWorkspace;

        public SignatureHelpController (
            RoslynCompilationWorkspace compilationWorkspace)
        {
            this.compilationWorkspace = compilationWorkspace
                ?? throw new ArgumentNullException (nameof (compilationWorkspace));
        }

        public async Task<SignatureHelp> ComputeSignatureHelpAsync (
            SourceText sourceText,
            Position position,
            CancellationToken cancellationToken)
        {
            var sourcePosition = sourceText.Lines.GetPosition (position.ToRoslyn ());
            if (sourcePosition <= 0)
                return default;

            var document = compilationWorkspace.GetSubmissionDocument (sourceText.Container);
            var root = await document.GetSyntaxRootAsync (cancellationToken);
            var syntaxToken = root.FindToken (sourcePosition);

            var semanticModel = await document.GetSemanticModelAsync (cancellationToken);

            var currentNode = syntaxToken.Parent;
            do {
                var creationExpression = currentNode as ObjectCreationExpressionSyntax;
                if (creationExpression != null && creationExpression.ArgumentList.Span.Contains (sourcePosition))
                    return CreateMethodGroupSignatureHelp (
                        creationExpression,
                        creationExpression.ArgumentList,
                        sourcePosition,
                        semanticModel);

                var invocationExpression = currentNode as InvocationExpressionSyntax;
                if (invocationExpression != null && invocationExpression.ArgumentList.Span.Contains (sourcePosition))
                    return CreateMethodGroupSignatureHelp (
                        invocationExpression.Expression,
                        invocationExpression.ArgumentList,
                        sourcePosition,
                        semanticModel);

                currentNode = currentNode.Parent;
            } while (currentNode != null);

            return default;
        }

        static SignatureHelp CreateMethodGroupSignatureHelp (
            ExpressionSyntax expression,
            ArgumentListSyntax argumentList,
            int position,
            SemanticModel semanticModel)
        {
            // Happens for object initializers with no preceding parens, as soon as user types comma
            if (argumentList == null)
                return default;

            TryGetCurrentArgumentIndex (argumentList, position, out var activeParameterIndex);

            var symbolInfo = semanticModel.GetSymbolInfo (expression);
            var bestGuessMethod = symbolInfo.Symbol as IMethodSymbol;

            // Include everything by default (global eval context)
            var includeInstance = true;
            var includeStatic = true;

            ITypeSymbol throughType = null;

            // When accessing method via some member, only show static methods in static context and vice versa for instance methods.
            // This block based on https://github.com/dotnet/roslyn/blob/3b6536f4a616e5f3b8ede940c63663a828e68b5d/src/Features/CSharp/Portable/SignatureHelp/InvocationExpressionSignatureHelpProvider_MethodGroup.cs#L44-L50
            if (expression is MemberAccessExpressionSyntax memberAccessExpression) {
                var throughExpression = (memberAccessExpression).Expression;
                if (!(throughExpression is BaseExpressionSyntax))
                    throughType = semanticModel.GetTypeInfo (throughExpression).Type;
                var throughSymbolInfo = semanticModel.GetSymbolInfo (throughExpression);
                var throughSymbol = throughSymbolInfo.Symbol ?? throughSymbolInfo.CandidateSymbols.FirstOrDefault ();

                includeInstance = !throughExpression.IsKind (SyntaxKind.IdentifierName) ||
                    semanticModel.LookupSymbols (throughExpression.SpanStart, name: throughSymbol.Name).Any (s => !(s is INamedTypeSymbol)) ||
                    (!(throughSymbol is INamespaceOrTypeSymbol) && semanticModel.LookupSymbols (throughExpression.SpanStart, throughSymbol.ContainingType).Any (s => !(s is INamedTypeSymbol)));
                includeStatic = throughSymbol is INamedTypeSymbol ||
                    (throughExpression.IsKind (SyntaxKind.IdentifierName) &&
                    semanticModel.LookupNamespacesAndTypes (throughExpression.SpanStart, name: throughSymbol.Name).Any (t => t.GetSymbolType () == throughType));
            }

            // TODO: Start taking CT in here? Most calls in this method have optional CT arg. Could make this async.
            var within = semanticModel.GetEnclosingNamedTypeOrAssembly (position, CancellationToken.None);

            var methods = semanticModel
                .GetMemberGroup (expression)
                .OfType<IMethodSymbol> ()
                .Where (m => (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance))
                .Where (m => m.IsAccessibleWithin (within, throughTypeOpt: throughType))
                .ToArray ();

            var activeSignatureIndex = 0;
            var signatures = new List<SignatureInformation> ();

            for (var i = 0; i < methods.Length; i++) {
                var method = methods [i];

                if (method == bestGuessMethod)
                    activeSignatureIndex = i;

                var parameters = method
                    .Parameters
                    .Select (p => new ParameterInformation (p.ToDisplayString (Constants.SymbolDisplayFormat)))
                    .ToList ();

                signatures.Add (new SignatureInformation (
                    method.ToDisplayString (Constants.SymbolDisplayFormat),
                    parameters));
            }

            return new SignatureHelp (
                signatures,
                activeSignatureIndex,
                activeParameterIndex);
        }

        // Pared down from Roslyn's internal CommonSignatureHelpUtilities (which covers more types of argument
        // lists than this version)
        static bool TryGetCurrentArgumentIndex (
            ArgumentListSyntax argumentList,
            int position,
            out int index)
        {
            index = 0;
            if (position < argumentList.OpenParenToken.Span.End)
                return false;

            var closeToken = argumentList.CloseParenToken;
            if (!closeToken.IsMissing && position > closeToken.SpanStart)
                return false;

            foreach (var element in argumentList.Arguments.GetWithSeparators ())
                if (element.IsToken && position >= element.Span.End)
                    index++;

            return true;
        }
    }
}