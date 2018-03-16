//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class TargetCompilationConfiguration
    {
        public string GlobalStateTypeName { get; set; }
        public AssemblyDefinition GlobalStateAssembly { get; set; }
        public string[] DefaultUsings { get; set; }
        public string[] DefaultWarningSuppressions { get; set; }
        public EvaluationContextId EvaluationContextId { get; set; }
    }
}