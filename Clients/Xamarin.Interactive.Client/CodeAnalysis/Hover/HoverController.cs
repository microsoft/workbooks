//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn
{
    sealed class HoverController
    {
        const string TAG = nameof (HoverController);

        readonly RoslynCompilationWorkspace compilationWorkspace;

        public HoverController (
            RoslynCompilationWorkspace compilationWorkspace)
        {
            this.compilationWorkspace = compilationWorkspace
                ?? throw new ArgumentNullException (nameof (compilationWorkspace));
        }

        public async Task<Hover> ProvideHoverAsync (
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

            var expression = syntaxToken.Parent as ExpressionSyntax;
            if (expression == null)
                return default;

            var semanticModel = await document.GetSemanticModelAsync (cancellationToken);
            var symbolInfo = semanticModel.GetSymbolInfo (expression);
            if (symbolInfo.Symbol == null)
                return default;

            return new Hover (
                syntaxToken
                    .GetLocation ()
                    .GetLineSpan ()
                    .Span
                    .FromRoslyn (),
                new [] { symbolInfo.Symbol.ToDisplayString (Constants.SymbolDisplayFormat) }
            );
        }
    }
}