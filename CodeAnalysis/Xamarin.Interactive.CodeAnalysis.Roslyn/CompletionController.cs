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
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;

using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.CodeAnalysis.Roslyn.Internals;
using Xamarin.Interactive.Logging;

using CompletionItem = Xamarin.Interactive.CodeAnalysis.Models.CompletionItem;
using CompletionItemKind = Xamarin.Interactive.CodeAnalysis.Models.CompletionItemKind;

using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn
{
    sealed class CompletionController
    {
        const string TAG = nameof (CompletionController);

        public const string ItemDetailPropertyName = "xi-itemdetail";

        readonly RoslynCompilationWorkspace compilationWorkspace;

        ModelComputation<CompletionModel> computation;

        public CompletionController (RoslynCompilationWorkspace compilationWorkspace)
            => this.compilationWorkspace = compilationWorkspace
                ?? throw new ArgumentNullException (nameof (compilationWorkspace));

        public async Task<IEnumerable<CompletionItem>> ProvideFilteredCompletionItemsAsync (
            Document document,
            LinePosition position,
            CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync (cancellationToken);

            Task<CompletionModel> ProvideCompletionItemsAsync ()
            {
                var sourcePosition = sourceText.Lines.GetPosition (position);
                var rules = compilationWorkspace.CompletionService.GetRules ();

                StopComputation ();
                StartNewComputation (
                    document,
                    sourceText,
                    sourcePosition,
                    rules,
                    filterItems: true);

                var currentComputation = computation;
                cancellationToken.Register (() => currentComputation.Stop ());

                return computation.ModelTask;
            }

            var model = await ProvideCompletionItemsAsync ();

            if (model?.FilteredItems == null)
                return Enumerable.Empty<CompletionItem> ();

            return model
                .FilteredItems
                .Where (i => i.Span.End <= sourceText.Length)
                .Select (i => {
                    i.Properties.TryGetValue ("InsertionText", out var insertionText);
                    i.Properties.TryGetValue (CompletionController.ItemDetailPropertyName, out var itemDetail);
                    return new CompletionItem (
                        ConversionExtensions.ToCompletionItemKind (i.Tags),
                        i.DisplayText,
                        insertionText,
                        itemDetail);
                });
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
            Document document,
            SourceText sourceText,
            int position,
            CompletionRules rules,
            bool filterItems)
        {
            computation = new ModelComputation<CompletionModel> (
                model => {
                    if (model == null)
                        StopComputation ();
                },
                PrioritizedTaskScheduler.AboveNormalInstance);

            ComputeModel (document, sourceText, position);

            if (filterItems)
                FilterModel (
                    CompletionHelper.GetHelper (document),
                    document,
                    sourceText);
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

        void ComputeModel (Document document, SourceText sourceText, int position)
        {
            if (computation.InitialUnfilteredModel != null)
                return;

            computation.ChainTask ((model, ct) => {
                if (model != null)
                    return Task.FromResult (model);

                return ComputeModelAsync (
                    compilationWorkspace,
                    document,
                    sourceText,
                    position,
                    ct);
            });
        }

        void FilterModel (
            CompletionHelper helper,
            Document document,
            SourceText sourceText)
            => computation.ChainTask ((model, ct) => FilterModelAsync (
                    compilationWorkspace,
                    document,
                    sourceText,
                    model,
                    helper,
                    ct));

        static async Task<CompletionModel> ComputeModelAsync (
            RoslynCompilationWorkspace compilationWorkspace,
            Document document,
            SourceText sourceText,
            int position,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested ();

            var completions = await compilationWorkspace.CompletionService.GetCompletionsAsync (
                document,
                position,
                options: compilationWorkspace.Options,
                cancellationToken: ct).ConfigureAwait (false);

            if (completions == null)
                return null;

            // TODO: Default tracking span
            //var trackingSpan = await _completionService.GetDefaultTrackingSpanAsync(_documentOpt, _subjectBufferCaretPosition, cancellationToken).ConfigureAwait(false);

            return CompletionModel.CreateModel (
                sourceText,
                default (TextSpan),
                completions.Items);
        }

        /// <summary>
        /// Filter currentCompletionList according to the current filter text. Roslyn's completion does not
        /// handle this automatically.
        /// </summary>
        static async Task<CompletionModel> FilterModelAsync (
            RoslynCompilationWorkspace compilationWorkspace,
            Document document,
            SourceText sourceText,
            CompletionModel model,
            CompletionHelper helper,
            CancellationToken ct)
        {
            if (model == null)
                return null;

            RoslynCompletionItem bestFilterMatch = null;
            var bestFilterMatchIndex = 0;

            var newFilteredCompletions = new List<RoslynCompletionItem> ();

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
                    itemDetail = overloads [0].ToDisplayString (Constants.SymbolDisplayFormat);

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
        static string GetFilterText (SourceText sourceText, RoslynCompletionItem completion)
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