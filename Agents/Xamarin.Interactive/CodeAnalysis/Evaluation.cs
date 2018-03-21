//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Representations.Reflection;

using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class Evaluation : IXipResponseMessage, IEvaluation
    {
        [NonSerialized]
        Compilation compilation;
        ICompilation IEvaluation.Compilation => compilation;
        public Compilation Compilation {
            set => compilation = value;
        }

        public Guid RequestId { get; set; }
        public CodeCellId CodeCellId { get; set; }
        public EvaluationPhase Phase { get; set; }
        public EvaluationResultHandling ResultHandling { get; set; }
        public object Result { get; set; }
        public ExceptionNode Exception { get; set; }
        public bool Interrupted { get; set; }
        public TimeSpan EvaluationDuration { get; set; }
        // CultureInfo is serializable, but it's very very heavy, and
        // does not cache (e.g. is not IObjectReference) so so we'll
        // just look up by LCID on the other side
        public int CultureLCID { get; set; }
        public int UICultureLCID { get; set; }
        public bool InitializedAgentIntegration { get; set; }
        public AssemblyDefinition [] LoadedAssemblies { get; set; }
    }
}