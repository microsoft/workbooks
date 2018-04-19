//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
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
        Append,

        /// <summary>
        /// Ignore this result. (e.g. perhaps the code that produced this result
        /// was not an expression and did not actually produce a value)
        /// </summary>
        Ignore
    }
}