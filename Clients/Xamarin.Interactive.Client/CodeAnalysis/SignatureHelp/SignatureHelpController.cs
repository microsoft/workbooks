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

using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.RoslynInternals;

namespace Xamarin.Interactive.CodeAnalysis.SignatureHelp
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

        public async Task<SignatureHelpViewModel> ComputeSignatureHelpAsync (
            SourceText sourceText,
            LinePosition linePosition,
            CancellationToken cancellationToken)
        {
            var signatureHelp = new SignatureHelpViewModel ();
            var position = sourceText.Lines.GetPosition (linePosition);

            if (position <= 0)
                return signatureHelp;

            var document = compilationWorkspace.GetSubmissionDocument (sourceText.Container);
            var root = await document.GetSyntaxRootAsync (cancellationToken);
            var syntaxToken = root.FindToken (position);

            var semanticModel = await document.GetSemanticModelAsync (cancellationToken);

            var currentNode = syntaxToken.Parent;
            do {
                var creationExpression = currentNode as ObjectCreationExpressionSyntax;
                if (creationExpression != null && creationExpression.ArgumentList.Span.Contains (position))
                    return CreateMethodGroupSignatureHelp (
                        creationExpression,
                        creationExpression.ArgumentList,
                        position,
                        semanticModel);

                var invocationExpression = currentNode as InvocationExpressionSyntax;
                if (invocationExpression != null && invocationExpression.ArgumentList.Span.Contains (position))
                    return CreateMethodGroupSignatureHelp (
                        invocationExpression.Expression,
                        invocationExpression.ArgumentList,
                        position,
                        semanticModel);

                currentNode = currentNode.Parent;
            } while (currentNode != null);

            return signatureHelp;
        }

        static SignatureHelpViewModel CreateMethodGroupSignatureHelp (
            ExpressionSyntax expression,
            ArgumentListSyntax argumentList,
            int position,
            SemanticModel semanticModel)
        {
            var signatureHelp = new SignatureHelpViewModel ();

            // Happens for object initializers with no preceding parens, as soon as user types comma
            if (argumentList == null)
                return signatureHelp;

            int currentArg;
            if (TryGetCurrentArgumentIndex (argumentList, position, out currentArg))
                signatureHelp.ActiveParameter = currentArg;

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

            var signatures = new List<SignatureViewModel> ();

            for (var i = 0; i < methods.Length; i++) {
                if (methods [i] == bestGuessMethod)
                    signatureHelp.ActiveSignature = i;

                var signatureInfo = new SignatureViewModel (methods [i]);

                signatures.Add (signatureInfo);
            }

            signatureHelp.Signatures = signatures.ToArray ();

            return signatureHelp;
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