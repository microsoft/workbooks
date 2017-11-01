//
// IEvaluation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace Xamarin.Interactive.CodeAnalysis
{
	/// <summary>
	/// The state of an evaluation taking place for a single cell in a workbook.
	/// </summary>
	public interface IEvaluation
	{
		/// <summary>
		/// The compilation that produced this evaluation.
		/// </summary>
		ICompilation Compilation { get; }

		/// <summary>
		/// The current phase of evaluation for the cell.
		/// </summary>
		EvaluationPhase Phase { get; }

		/// <summary>
		/// The current result of an evaluation. Depending on <see cref="Phase"/>, the
		/// value will be unset, the raw value after evaluation, or the transformed
		/// representation value.
		object Result { get; }
	}
}