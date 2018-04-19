//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    /// <summary>
    /// The phase of an evaluation taking place for a single cell in a workbook.
    /// </summary>
    public enum EvaluationPhase
    {
        /// <summary>
        /// This should never be seen in practice.
        /// </summary>
        None,

        /// <summary>
        /// This is the initial state for an evaluation in flight - the compiled
        /// cell (its IL and metadata) has been received by the agent and is about
        /// to be evaluated.
        /// </summary>
        Compiled,

        /// <summary>
        /// The cell has been evaluated and <see cref="IEvaluation.Result"/> may be
        /// acted upon. The result has not yet been transformed through
        /// <see cref="IAgent.RepresentationManager"/>.
        /// </summary>
        Evaluated,

        /// <summary>
        /// The cell's evaluation result as been fully transformed through
        /// <see cref="IAgent.RepresentationManager"/>, and <see cref="IEvaluation.Result"/>
        /// has been updated to reflect that. The evaluation has been published to
        /// the client for rendering.
        /// </summary>
        Completed
    }
}