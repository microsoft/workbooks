// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Protocol
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

            var assemblies = new List<AssemblyDefinition> ();

            foreach (var asm in Agent.AppDomainStartupAssemblies) {
                if (!asm.IsDynamic && !String.IsNullOrEmpty (asm.Location)) {
                    // HACK: This is a temporary fix to get iOS agent/app assemblies sent to the
                    //       Windows client when using the remote sim.
                    var peImage = includePEImagesInDependencyResolution && File.Exists (asm.Location)
                        ? File.ReadAllBytes (asm.Location)
                        : null;

                    assemblies.Add (new AssemblyDefinition (
                        new AssemblyIdentity (asm.GetName ()),
                        asm.Location,
                        peImage: peImage));
                }
            }

            return Task.FromResult (Configuration.With (
                evaluationContextId: evaluationContext.Id,
                globalStateType: globalStateTypeDefinition,
                defaultImports: agent.GetReplDefaultUsingNamespaces ().ToArray (),
                defaultWarningSuppressions: agent.GetReplDefaultWarningSuppressions ().ToArray (),
                initialReferences: assemblies,
                includePEImagesInDependencyResolution: includePEImagesInDependencyResolution));
        }
    }
}