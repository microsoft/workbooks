// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis
{

    /// <summary>
    /// The state of an evaluation taking place for a single cell in a workbook.
    /// </summary>
    public struct EvaluationInFlight
    {

        /// <summary>
        /// The current phase of evaluation for the cell.
        /// </summary>
        public EvaluationPhase Phase { get; }


        /// <summary>
        /// The compilation that produced this evaluation.
        /// </summary>
        public ICompilation Compilation { get; }

        /// <summary>
        /// The original raw result value produced this evaluation. This will be
        /// set when <see cref="Phase"/> is set to <c>Evaluated</c>.
        /// </summary>
        public object OriginalValue { get; }

        /// <summary>
        /// The final result that will be posted back to the client. This will
        /// be set when <see cref="Phase"/> is set to <c>Represented</c> and will
        /// contain any representations of <see cref="OriginalValue"/>, if any.
        /// </summary>
        public Evaluation Evaluation { get; }

        internal EvaluationInFlight (
            EvaluationPhase phase,
            ICompilation compilation = null,
            object originalValue = null,
            Evaluation evaluation = null)
        {
            Phase = phase;
            Compilation = compilation;
            OriginalValue = originalValue;
            Evaluation = evaluation;
        }

        internal EvaluationInFlight With (
            Optional<EvaluationPhase> phase = default,
            Optional<ICompilation> compilation = default,
            Optional<object> originalValue = default,
            Optional<Evaluation> evaluation = default)
            => new EvaluationInFlight (
                phase.GetValueOrDefault (Phase),
                compilation.GetValueOrDefault (Compilation),
                originalValue.GetValueOrDefault (OriginalValue),
                evaluation.GetValueOrDefault (Evaluation));
    }
}