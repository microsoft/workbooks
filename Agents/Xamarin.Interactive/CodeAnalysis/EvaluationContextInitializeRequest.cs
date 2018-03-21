//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class EvaluationContextInitializeRequest : MainThreadRequest<TargetCompilationConfiguration>
    {
        public bool IncludePeImage { get; }

        public EvaluationContextInitializeRequest (bool includePeImage)
        {
            IncludePeImage = includePeImage;
        }

        protected override Task<TargetCompilationConfiguration> HandleAsync (Agent agent)
        {
            var response = new TargetCompilationConfiguration ();

            response.DefaultUsings = agent.GetReplDefaultUsingNamespaces ().ToArray ();
            response.DefaultWarningSuppressions = agent.GetReplDefaultWarningSuppressions ().ToArray ();

            var evaluationContext = agent.CreateEvaluationContext ();
            response.EvaluationContextId = evaluationContext.Id;

            if (evaluationContext.GlobalState != null) {
                var globalStateType = evaluationContext.GlobalState.GetType ();
                response.GlobalStateTypeName = globalStateType.FullName;

                // HACK: This is a temporary fix to get iOS agent/app assemblies sent to the
                //       Windows client when using the remote sim.
                var peImage = IncludePeImage && File.Exists (globalStateType.Assembly.Location)
                    ? File.ReadAllBytes (globalStateType.Assembly.Location)
                    : null;

                response.GlobalStateAssembly = new AssemblyDefinition (
                    new AssemblyIdentity (globalStateType.Assembly.GetName ()),
                    globalStateType.Assembly.Location,
                    peImage: peImage);
            }

            return Task.FromResult (response);
        }
    }
}