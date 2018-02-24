//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis.Events
{
    sealed class CodeCellResultEvent : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public object Value { get; }
        public EvaluationResultHandling ResultHandling { get; }

        public CodeCellResultEvent (CodeCellId codeCellId, object value, EvaluationResultHandling resultHandling)
        {
            CodeCellId = codeCellId;
            Value = value;
            ResultHandling = resultHandling;
        }
    }
}