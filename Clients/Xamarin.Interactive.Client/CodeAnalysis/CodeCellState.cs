//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.CodeAnalysis
{
    sealed class CodeCellState
    {
        public CodeCellId Id { get; }
        public CodeCellBuffer Buffer { get; }

        readonly List<InteractiveDiagnostic> diagnostics = new List<InteractiveDiagnostic> ();
        public IReadOnlyList<InteractiveDiagnostic> Diagnostics => diagnostics;

        public bool IsOutdated { get; set; }
        public bool IsDirty { get; set; }

        public bool IsEvaluating { get; set; }
        public bool AgentTerminatedWhileEvaluating { get; private set; }
        public int EvaluationCount { get; private set; }
        public Guid LastEvaluationRequestId { get; set; }
        public bool IsResultAnExpression { get; set; }

        public CodeCellState (
            CodeCellId id,
            CodeCellBuffer buffer)
        {
            Id = id;
            Buffer = buffer;
        }

        public bool IsEvaluationCandidate => IsDirty || IsOutdated || EvaluationCount == 0;

        public void NotifyEvaluated (bool agentTerminatedWhileEvaluating)
        {
            EvaluationCount++;
            AgentTerminatedWhileEvaluating = agentTerminatedWhileEvaluating;
            IsDirty = false;
            IsOutdated = false;
            IsEvaluating = false;
        }

        public void Reset ()
        {
            diagnostics.Clear ();
        }

        public void AppendDiagnostic (InteractiveDiagnostic diagnostic)
            => diagnostics.Add (diagnostic);
    }
}