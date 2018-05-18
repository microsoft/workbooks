//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.Web.WebAssembly
{
    // TODO: Implement evaluation abort and specific assembly load (used for integrations only). - brajkovic
    sealed class WebAssemblyEvaluationContextManager : IEvaluationContextManager
    {
        const string workbookAppId = "webassembly-monowebassembly";
        static readonly string [] defaultImports = {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Threading",
            "System.Threading.Tasks",
            "Xamarin.Interactive.CodeAnalysis.Workbooks"
        };

        readonly Observable<ICodeCellEvent> events = new Observable<ICodeCellEvent> ();
        readonly IReadOnlyList<string> assemblySearchPaths;
        readonly Sdk webAssemblySdk;
        readonly string appPath;

        public IObservable<ICodeCellEvent> Events => events;

        public WebAssemblyEvaluationContextManager ()
        {
            var workbookApp = WorkbookAppInstallation.LookupById (workbookAppId);
            appPath = workbookApp.AppPath;
            webAssemblySdk = workbookApp.Sdk;
            this.assemblySearchPaths = webAssemblySdk.AssemblySearchPaths;
        }

        public Task AbortEvaluationAsync (
            EvaluationContextId evaluationContextId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (
            CancellationToken cancellationToken = default)
            => CreateEvaluationContextAsync (
                TargetCompilationConfiguration.CreateInitialForCompilationWorkspace (),
                cancellationToken);

        public Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (
            TargetCompilationConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            var evaluationContextId = configuration.EvaluationContextId;
            if (evaluationContextId == default)
                evaluationContextId = EvaluationContextId.Create ();

            // On WASM, we'll have loaded the following assemblies by default:
            var assemblyBase = new FilePath (appPath);
            var defaultAssemblies = new [] {
                (
                    name: "mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                    path: assemblyBase.Combine ("BCL").Combine ("mscorlib.dll")
                ),
                (
                    name: "netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
                    path: assemblyBase.Combine ("BCL").Combine ("Facades").Combine ("netstandard.dll")
                ),
                (
                    name: "System, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                    path: assemblyBase.Combine ("BCL").Combine ("System.dll")
                ),
                (
                    name: "System.Core, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                    path: assemblyBase.Combine ("BCL").Combine ("System.Core.dll")
                ),
                (
                    name: "Xamarin.Interactive, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    path: assemblyBase.Combine ("Xamarin.Interactive.dll")
                ),
                (
                    name: "Xamarin.Workbooks.WebAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    path: assemblyBase.Combine ("Xamarin.Workbooks.WebAssembly.dll")
                )
            };

            var assemblies = defaultAssemblies.Select (
                da => new AssemblyDefinition (new AssemblyName (da.name), da.path))
                .ToList ();
            var interactiveAssembly = assemblies.Single (a => a.Name.Name == "Xamarin.Interactive");
            var globalStateTypeDefinition = new TypeDefinition (
                interactiveAssembly,
                typeof (EvaluationContextGlobalObject).FullName);

            configuration = configuration
                .With (
                    evaluationContextId: evaluationContextId,
                    globalStateType: globalStateTypeDefinition,
                    initialReferences: assemblies,
                    assemblySearchPaths: new Optional<IReadOnlyList<string>> (this.assemblySearchPaths),
                    sdk: webAssemblySdk,
                    defaultImports: defaultImports);

            return Task.FromResult (configuration);
        }

        public Task EvaluateAsync (
            EvaluationContextId evaluationContextId,
            Compilation compilation,
            CancellationToken cancellationToken = default)
        {
            events.Observers.OnNext (compilation);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AssemblyLoadResult>> LoadAssembliesAsync (
            EvaluationContextId evaluationContextId,
            IReadOnlyList<AssemblyDefinition> assemblies,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task ResetStateAsync (
            EvaluationContextId evaluationContextId,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}