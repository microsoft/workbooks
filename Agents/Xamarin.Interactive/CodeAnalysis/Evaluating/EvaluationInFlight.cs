// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.CodeAnalysis.Events;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    /// <summary>
    /// The state of an evaluation taking place for a single cell in a workbook.
    /// </summary>
    public sealed class EvaluationInFlight : ICodeCellEvent
    {
        internal static EvaluationInFlight Create (Compilation compilation)
            => new EvaluationInFlight (
                compilation.CodeCellId,
                EvaluationPhase.Compiled,
                compilation);

        /// <summary>
        /// An identifier that is unique to the cell being evaluated. This should be
        /// passed to <see cref="IAgent.PublishEvaluation"/>.
        /// </summary>
        public CodeCellId CodeCellId { get; }

        /// <summary>
        /// The current phase of evaluation for the cell.
        /// </summary>
        public EvaluationPhase Phase { get; }

        /// <summary>
        /// The compilation that produced this evaluation.
        /// </summary>
        public Compilation Compilation { get; }

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

        EvaluationInFlight (
            CodeCellId codeCellId,
            EvaluationPhase phase,
            Compilation compilation = null,
            object originalValue = null,
            Evaluation evaluation = null)
        {
            CodeCellId = codeCellId;
            Phase = phase;
            Compilation = compilation;
            OriginalValue = originalValue;
            Evaluation = evaluation;
        }

        internal EvaluationInFlight With (
            Optional<EvaluationPhase> phase = default,
            Optional<Compilation> compilation = default,
            Optional<object> originalValue = default,
            Optional<Evaluation> evaluation = default)
            => new EvaluationInFlight (
                CodeCellId,
                phase.GetValueOrDefault (Phase),
                compilation.GetValueOrDefault (Compilation),
                originalValue.GetValueOrDefault (OriginalValue),
                evaluation.GetValueOrDefault (Evaluation));
    }
}