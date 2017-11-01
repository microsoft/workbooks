//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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