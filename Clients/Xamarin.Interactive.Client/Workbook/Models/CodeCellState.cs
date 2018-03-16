//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Microsoft.CodeAnalysis;

using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Workbook.Models
{
    sealed class CodeCellState
    {
        public CodeCell Cell { get; }

        public IEditor Editor { get; set; }
        public ICodeCellView View { get; set; }
        public RoslynCompilationWorkspace CompilationWorkspace { get; set; }
        public DocumentId DocumentId { get; set; }
        public bool IsResultAnExpression { get; set; }

        public int EvaluationCount { get; private set; }
        public bool AgentTerminatedWhileEvaluating { get; private set; }

        public CodeCellState (CodeCell cell)
            => Cell = cell ?? throw new ArgumentNullException (nameof (cell));

        public bool IsFrozen => View.IsFrozen;
        public void Freeze () => View.Freeze ();

        public void NotifyEvaluated (bool agentTerminatedWhileEvaluating)
        {
            EvaluationCount++;
            AgentTerminatedWhileEvaluating = agentTerminatedWhileEvaluating;
            View.IsDirty = false;
            View.IsOutdated = false;
            View.IsEvaluating = false;
        }
    }
}