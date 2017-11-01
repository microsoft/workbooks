//
// EvaluationPhase.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace Xamarin.Interactive.CodeAnalysis
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
		/// has been updated to reflect that.
		/// </summary>
        Represented,

        /// <summary>
		/// Cell evaluation is complete and the final result has been published to
		/// the client for rendering.
		/// </summary>
        Completed
    }
}