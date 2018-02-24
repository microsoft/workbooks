//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis.Events
{
    sealed class CodeCellStateUpdatedEvent : IEvaluationEvent
    {
        public CodeCellId CodeCellId { get; }
        public CodeCellState CodeCellState { get; }

        public CodeCellStateUpdatedEvent (CodeCellState codeCellState)
        {
            if (codeCellState == null)
                throw new ArgumentNullException (nameof (codeCellState));

            CodeCellId = codeCellState.Id;
            CodeCellState = codeCellState;
        }
    }
}