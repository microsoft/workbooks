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

using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis.Hover
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

        public async Task<HoverViewModel> ProvideHoverAsync (
            SourceText sourceText,
            LinePosition linePosition,
            CancellationToken cancellationToken)
        {
            var hover = new HoverViewModel ();
            var position = sourceText.Lines.GetPosition (linePosition);

            if (position <= 0)
                return hover;

            var document = compilationWorkspace.GetSubmissionDocument (sourceText.Container);
            var root = await document.GetSyntaxRootAsync (cancellationToken);
            var syntaxToken = root.FindToken (position);

            var expression = syntaxToken.Parent as ExpressionSyntax;
            if (expression == null)
                return hover;

            var semanticModel = await document.GetSemanticModelAsync (cancellationToken);
            var symbolInfo = semanticModel.GetSymbolInfo (expression);
            if (symbolInfo.Symbol == null)
                return hover;

            hover.Contents = new [] { symbolInfo.Symbol.ToDisplayString (Constants.SymbolDisplayFormat) };
            hover.Range = syntaxToken.GetLocation ().GetLineSpan ().Span;

            return hover;
        }
    }
}