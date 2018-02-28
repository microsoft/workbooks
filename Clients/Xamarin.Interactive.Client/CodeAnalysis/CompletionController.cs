//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.RoslynInternals;

namespace Xamarin.Interactive.CodeAnalysis
{
    sealed class CompletionController
    {
        const string TAG = nameof (CompletionController);

        public const string ItemDetailPropertyName = "xi-itemdetail";

        // TODO: Move this somewhere shared. Copied from MonacoExtensions
        static readonly SymbolDisplayFormat symbolDisplayFormat = SymbolDisplayFormat.CSharpErrorMessageFormat
            .WithParameterOptions (
                SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeParamsRefOut)
            .WithMemberOptions (
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeContainingType |
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeRef |
                SymbolDisplayMemberOptions.IncludeExplicitInterface);

        readonly RoslynCompilationWorkspace compilationWorkspace;

        SourceText sourceTextContent;
        ModelComputation<CompletionModel> computation;
        CompletionModel lastCompletionModel;

        public CompletionController (RoslynCompilationWorkspace compilationWorkspace)
        {
            this.compilationWorkspace = compilationWorkspace;
        }

        public Task<CompletionModel> ProvideCompletionItemsAsync (
            SourceText sourceText,
            LinePosition linePosition,
            CancellationToken cancellationToken)
        {
            this.sourceTextContent = sourceText;

            var sourcePosition = sourceTextContent.Lines.GetPosition (linePosition);
            var rules = compilationWorkspace.CompletionService.GetRules ();

            StopComputation ();
            StartNewComputation (sourcePosition, rules, filterItems: true);

            var currentComputation = computation;
            cancellationToken.Register (() => currentComputation.Stop ());

            return computation.ModelTask;
        }

        public async Task<IEnumerable<CompletionItemViewModel>> ProvideFilteredCompletionItemsAsync (
            SourceText sourceText,
            LinePosition linePosition,
            CancellationToken cancellationToken)
        {
            var model = await ProvideCompletionItemsAsync (sourceText, linePosition, cancellationToken);

            return model
                ?.FilteredItems
                ?.Where (i => i.Span.End <= model.Text.Length)
                ?.Select (i => new CompletionItemViewModel (i));
        }

        void OnCompletionModelUpdated (CompletionModel model)
        {
            if (model == null) {
                StopComputation ();
                return;
            }

            if (lastCompletionModel == model)
                return;

            lastCompletionModel = model;
        }

        /// <summary>
        /// Start a new ModelComputation. Completion computations and filtering tasks will be chained to this
        /// ModelComputation, and when the chain is complete the view will be notified via the
        /// OnCompletionModelUpdated handler.
        /// 
        /// The latest immutable CompletionModel can be accessed at any time. Some parts of the code may choose
        /// to wait on it to arrive synchronously.
        /// 
        /// Inspired by similar method in Completion/Controller.cs in Roslyn.
        /// </summary>
        void StartNewComputation (
            int position,
            CompletionRules rules,
            bool filterItems)
        {
            computation = new ModelComputation<CompletionModel> (
                OnCompletionModelUpdated,
                Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.PrioritizedTaskScheduler.AboveNormalInstance);

            ComputeModel (position);

            if (filterItems) {
                var document = compilationWorkspace.GetSubmissionDocument (sourceTextContent.Container);
                FilterModel (CompletionHelper.GetHelper (document));
            }
        }

        void StopComputation ()
        {
            computation?.Stop ();
            computation = null;
        }

        // TODO: MS doesn't worry about exceptions during the various waits it does. Remove this if we can.
        CompletionModel WaitForCompletionModel ()
        {
            try {
                return computation?.ModelTask?.Result;
            } catch (Exception e) {
                Log.Error (TAG, e);
                return null;
            }
        }

        void ComputeModel (
            int position)
        {
            if (computation.InitialUnfilteredModel != null)
                return;
            computation.ChainTask ((model, ct) => model != null ? Task.FromResult (model)
                : ComputeModelAsync (
                    compilationWorkspace,
                    sourceTextContent,
                    position,
                    ct));
        }

        void FilterModel (CompletionHelper helper)
        {
            computation.ChainTask ((model, ct) => FilterModelAsync (
                compilationWorkspace,
                sourceTextContent,
                model,
                helper,
                ct));
        }

        static async Task<CompletionModel> ComputeModelAsync (
            RoslynCompilationWorkspace compilationWorkspace,
            SourceText text,
            int position,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested ();

            var completions = await compilationWorkspace.CompletionService.GetCompletionsAsync (
                compilationWorkspace.GetSubmissionDocument (text.Container),
                position,
                options: compilationWorkspace.Options,
                cancellationToken: ct).ConfigureAwait (false);

            if (completions == null)
                return null;

            // TODO: Default tracking span
            //var trackingSpan = await _completionService.GetDefaultTrackingSpanAsync(_documentOpt, _subjectBufferCaretPosition, cancellationToken).ConfigureAwait(false);

            return CompletionModel.CreateModel (
                text,
                default (TextSpan),
                completions.Items);
        }

        /// <summary>
        /// Filter currentCompletionList according to the current filter text. Roslyn's completion does not
        /// handle this automatically.
        /// </summary>
        static async Task<CompletionModel> FilterModelAsync (
            RoslynCompilationWorkspace compilationWorkspace,
            SourceText sourceText,
            CompletionModel model,
            CompletionHelper helper,
            CancellationToken ct)
        {
            if (model == null)
                return null;

            CompletionItem bestFilterMatch = null;
            var bestFilterMatchIndex = 0;

            var document = compilationWorkspace.GetSubmissionDocument (sourceText.Container);
            var newFilteredCompletions = new List<CompletionItem> ();

            foreach (var item in model.TotalItems) {
                var completion = item;
                // TODO: Better range checking on delete before queuing up filtering (see TODO in HandleChange)
                if (completion.Span.Start > sourceText.Length)
                    continue;

                var filterText = GetFilterText (sourceText, completion);

                // CompletionRules.MatchesFilterText seems to always return false when filterText is
                // empty.
                if (filterText != String.Empty && !helper.MatchesPattern (
                    completion.FilterText,
                    filterText,
                    CultureInfo.CurrentCulture))
                    continue;

                var itemDetail = String.Empty;

                var symbols = await SymbolCompletionItem.GetSymbolsAsync (completion, document, ct)
                    .ConfigureAwait (false);
                var overloads = symbols.OfType<IMethodSymbol> ().ToArray ();
                if (overloads.Length > 0) {
                    itemDetail = overloads [0].ToDisplayString (symbolDisplayFormat);

                    if (overloads.Length > 1)
                        itemDetail += $" (+ {overloads.Length - 1} overload(s))";
                }

                completion = completion.AddProperty (ItemDetailPropertyName, itemDetail);

                newFilteredCompletions.Add (completion);

                if (bestFilterMatch == null || helper.CompareItems (
                    completion,
                    bestFilterMatch,
                    filterText,
                    CultureInfo.CurrentCulture) > 0) {
                    bestFilterMatch = completion;
                    bestFilterMatchIndex = newFilteredCompletions.Count - 1;
                }
            }

            if (newFilteredCompletions.Count == 0)
                return null;

            return model
                .WithFilteredItems (newFilteredCompletions)
                .WithSelectedItem (bestFilterMatch, bestFilterMatchIndex)
                .WithText (sourceText);
        }

        /// <summary>
        /// Get filterText suitable for being passed into CompletionRules.MatchesFilterText.
        /// 
        /// If currentCompletionList hasn't been updated (due to typing a single extra character), the
        /// CompletionItem.FilterSpan will be off by the amount of new characters. Adjust as necessary
        /// </summary>
        static string GetFilterText (SourceText sourceText, CompletionItem completion)
        {
            var filterSpan = completion.Span;
            var filterText = sourceText.GetSubText (filterSpan.Start).ToString ();
            for (var i = 0; i < filterText.Length; i++) {
                if (i < filterSpan.Length)
                    continue;
                var ch = filterText [i];
                if (!IsPotentialFilterCharacter (ch))
                    return filterText.Substring (0, i);
            }
            return filterText;
        }

        // Inspired by same method in Controller_TypeChar.cs in Roslyn
        static bool IsPotentialFilterCharacter (char ch)
        {
            return Char.IsLetterOrDigit (ch) || ch == '_';
        }
    }
}
