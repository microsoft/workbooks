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
        public TargetCompilationConfiguration Configuration { get; }

        public EvaluationContextInitializeRequest (TargetCompilationConfiguration configuration)
            => Configuration = configuration
                ?? throw new ArgumentNullException (nameof (configuration));

        protected override Task<TargetCompilationConfiguration> HandleAsync (Agent agent)
        {
            var evaluationContext = agent.CreateEvaluationContext ();

            var includePEImagesInDependencyResolution = agent.IncludePEImageInAssemblyDefinitions (
                Configuration.CompilationOS);

            TypeDefinition globalStateTypeDefinition = default;

            if (evaluationContext.GlobalState != null) {
                var globalStateType = evaluationContext.GlobalState.GetType ();
                byte [] peImage = null;

                if (includePEImagesInDependencyResolution &&
                    File.Exists (globalStateType.Assembly.Location))
                    peImage = File.ReadAllBytes (globalStateType.Assembly.Location);

                globalStateTypeDefinition = new TypeDefinition (
                    new AssemblyDefinition (
                        new AssemblyIdentity (globalStateType.Assembly.GetName ()),
                        globalStateType.Assembly.Location,
                        peImage: peImage),
                    globalStateType.FullName);
            }

            return Task.FromResult (Configuration.With (
                evaluationContextId: evaluationContext.Id,
                globalStateType: globalStateTypeDefinition,
                defaultImports: agent.GetReplDefaultUsingNamespaces ().ToArray (),
                defaultWarningSuppressions: agent.GetReplDefaultWarningSuppressions ().ToArray (),
                includePEImagesInDependencyResolution: includePEImagesInDependencyResolution));
        }
    }
}