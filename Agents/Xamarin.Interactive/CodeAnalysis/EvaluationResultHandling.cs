//
// EvaluationResultHandling.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace Xamarin.Interactive.CodeAnalysis
{
	/// <summary>
	/// Dictates how the client should handle rendering of the result.
	/// </summary>
	public enum EvaluationResultHandling
	{
		/// <summary>
		/// Any previous evaluation results for the cell will be replaced.
		/// </summary>
		Replace,

		/// <summary>
		/// The result will be appended to any previous evaluation results for the cell.
		/// </summary>
		Append
	}
}