//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class Compilation : ICompilation
    {
        IAssemblyDefinition ICompilation.Assembly => ExecutableAssembly;

        public CodeCellId CodeCellId { get; }
        public int SubmissionNumber { get; }
        public EvaluationContextId EvaluationContextId { get; }
        public bool IsResultAnExpression { get; }
        public IEvaluationEnvironment EvaluationEnvironment { get; }
        public AssemblyDefinition ExecutableAssembly { get; }
        public IReadOnlyList<AssemblyDefinition> References { get; }

        public Compilation (
            CodeCellId codeCellId,
            int submissionNumber,
            EvaluationContextId evaluationContextId,
            IEvaluationEnvironment evaluationEnvironment,
            bool isResultAnExpression,
            AssemblyDefinition executableAssembly,
            IReadOnlyList<AssemblyDefinition> references)
        {
            CodeCellId = codeCellId;
            SubmissionNumber = submissionNumber;
            EvaluationContextId = evaluationContextId;
            EvaluationEnvironment = evaluationEnvironment;
            IsResultAnExpression = isResultAnExpression;
            ExecutableAssembly = executableAssembly;
            References = references;
        }
    }
}