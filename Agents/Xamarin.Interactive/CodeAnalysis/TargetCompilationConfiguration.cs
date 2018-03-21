//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    public sealed class TargetCompilationConfiguration
    {
        public string GlobalStateTypeName { get; set; }
        public AssemblyDefinition GlobalStateAssembly { get; set; }
        public string[] DefaultUsings { get; set; }
        public string[] DefaultWarningSuppressions { get; set; }
        public EvaluationContextId EvaluationContextId { get; set; }
    }
}