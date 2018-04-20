// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    public class EvaluationContextManager : IEvaluationContextManager
    {
        const string TAG = nameof (EvaluationContextManager);

        static readonly Assembly [] appDomainStartupAssemblies = AppDomain.CurrentDomain.GetAssemblies ();

        static readonly string [] defaultImports = {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Threading",
            "System.Threading.Tasks",
            "Xamarin.Interactive.CodeAnalysis.Workbooks"
        };

        struct EvaluationContextRegistration : IDisposable
        {
            public EvaluationContext Context { get; }
            public IDisposable EventSubscription { get; }

            public EvaluationContextRegistration (
                EvaluationContext context,
                IDisposable eventSubscription)
            {
                Context = context;
                EventSubscription = eventSubscription;
            }

            public void Dispose ()
            {
                EventSubscription.Dispose ();
                Context.Dispose ();
            }
        }

        readonly Dictionary<EvaluationContextManagerIntegrationAttribute, IEvaluationContextManagerIntegration> loadedIntegrations
            = new Dictionary<EvaluationContextManagerIntegrationAttribute, IEvaluationContextManagerIntegration> ();

        readonly Dictionary<EvaluationContextId, EvaluationContextRegistration> evaluationContexts
            = new Dictionary<EvaluationContextId, EvaluationContextRegistration> ();

        internal object Context { get; }

        internal RepresentationManager InternalRepresentationManager { get; }
        public IRepresentationManager RepresentationManager => InternalRepresentationManager;

        readonly Observable<ICodeCellEvent> events = new Observable<ICodeCellEvent> ();
        public IObservable<ICodeCellEvent> Events => events;

        public IAgentSynchronizationContext SynchronizationContexts { get; }
            = new AgentSynchronizationContext ();

        readonly IList<Action> resetStateHandlers = new List<Action> ();

        internal EvaluationContextManager (
            RepresentationManager representationManager,
            object context = null)
        {
            InternalRepresentationManager = representationManager
                ?? throw new ArgumentNullException (nameof (representationManager));

            Context = context;
        }

        internal void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                foreach (var contextRegistration in evaluationContexts.Values)
                    contextRegistration.Dispose ();

                evaluationContexts.Clear ();
            }
        }

        public Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (
            CancellationToken cancellationToken = default)
            => CreateEvaluationContextAsync (
                TargetCompilationConfiguration.CreateInitialForCompilationWorkspace (),
                cancellationToken);

        public Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (
            TargetCompilationConfiguration targetCompilationConfiguration,
            CancellationToken cancellationToken = default)
        {
            if (targetCompilationConfiguration == null)
                throw new ArgumentNullException (nameof (targetCompilationConfiguration));

            var globalStateObject = CreateGlobalState ();

            targetCompilationConfiguration = targetCompilationConfiguration.With (
                defaultImports: defaultImports);

            targetCompilationConfiguration = PrepareTargetCompilationConfiguration (
                targetCompilationConfiguration);

            targetCompilationConfiguration = FinalizeTargetCompilationConfiguration (
                targetCompilationConfiguration,
                globalStateObject);

            var evaluationContext = new EvaluationContext (
                this,
                globalStateObject);

            if (globalStateObject is EvaluationContextGlobalObject evaluationContextGlobalObject)
                evaluationContextGlobalObject.EvaluationContext = evaluationContext;

            evaluationContexts.Add (
                targetCompilationConfiguration.EvaluationContextId,
                new EvaluationContextRegistration (
                    evaluationContext,
                    evaluationContext.Events.Subscribe (
                        new Observer<ICodeCellEvent> (events.Observers.OnNext))));

            OnEvaluationContextCreated (evaluationContext);

            return Task.FromResult (targetCompilationConfiguration);
        }

        internal virtual void LoadExternalDependencies (
            Assembly loadedAssembly,
            IReadOnlyList<AssemblyDependency> externalDependencies)
        {
        }

        internal bool TryLoadIntegration (Assembly assembly)
        {
            if (assembly?.GetReferencedAssemblies ().Any (r => r.Name == "Xamarin.Interactive") == false)
                return false;

            var integrationAttribute = assembly.GetCustomAttribute<EvaluationContextManagerIntegrationAttribute> ();
            var integrationType = integrationAttribute?.IntegrationType;

            if (integrationType == null)
                return false;

            if (loadedIntegrations.TryGetValue (integrationAttribute, out var integration))
                return false;

            if (!typeof (IEvaluationContextManagerIntegration).IsAssignableFrom (integrationType))
                throw new InvalidOperationException (
                    $"encountered [assembly:{typeof (IEvaluationContextManagerIntegration).FullName}" +
                    $"({integrationType.FullName})] on assembly '{assembly.FullName}', " +
                    $"but type specified does not implement " +
                    $"{typeof (IEvaluationContextManagerIntegration).FullName}");

            integration = (IEvaluationContextManagerIntegration)Activator.CreateInstance (integrationType);
            integration.IntegrateWith (this);

            loadedIntegrations.Add (integrationAttribute, integration);

            return true;
        }

        protected virtual object CreateGlobalState ()
            => new object ();

        protected virtual void OnEvaluationContextCreated (EvaluationContext evaluationContext)
        {
        }

        EvaluationContext GetEvaluationContext (EvaluationContextId evaluationContextId)
        {
            if (evaluationContexts.TryGetValue (evaluationContextId, out var registration))
                return registration.Context;

            throw new ArgumentException (
                $"No execution context found with session ID {evaluationContextId}");
        }

        public void RegisterResetStateHandler (Action handler)
            => resetStateHandlers.Add (handler
                ?? throw new ArgumentNullException (nameof (handler)));

        public Task ResetStateAsync (
            EvaluationContextId evaluationContextId,
            CancellationToken cancellationToken = default)
        {
            OnResetState ();

            foreach (var handler in resetStateHandlers) {
                try {
                    handler ();
                } catch (Exception e) {
                    Log.Error (TAG, "Registered reset state handler threw exception", e);
                }
            }

            return Task.CompletedTask;
        }

        protected virtual void OnResetState ()
        {
        }

        public void PublishValueForCell (
            CodeCellId codeCellId,
            object result,
            EvaluationResultHandling resultHandling = EvaluationResultHandling.Replace)
            => MainThread.Post (() => events.Observers.OnNext (new Evaluation (
                codeCellId,
                resultHandling,
                InternalRepresentationManager.Prepare (result))));

        protected virtual TargetCompilationConfiguration PrepareTargetCompilationConfiguration (
            TargetCompilationConfiguration configuration)
            => configuration;

        TargetCompilationConfiguration FinalizeTargetCompilationConfiguration (
            TargetCompilationConfiguration configuration,
            object globalStateObject)
        {
            var evaluationContextId = configuration.EvaluationContextId;
            if (evaluationContextId == default)
                evaluationContextId = EvaluationContextId.Create ();

            TypeDefinition globalStateTypeDefinition = default;

            if (globalStateObject != null) {
                var globalStateType = globalStateObject.GetType ();
                byte [] peImage = null;

                if (configuration.IncludePEImagesInDependencyResolution &&
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

            foreach (var assembly in appDomainStartupAssemblies) {
                if (!assembly.IsDynamic && !String.IsNullOrEmpty (assembly.Location)) {
                    // HACK: This is a temporary fix to get iOS agent/app assemblies sent to the
                    //       Windows client when using the remote sim.
                    byte [] peImage = null;
                    if (configuration.IncludePEImagesInDependencyResolution &&
                        File.Exists (assembly.Location))
                        peImage = File.ReadAllBytes (assembly.Location);

                    assemblies.Add (new AssemblyDefinition (
                        new AssemblyIdentity (assembly.GetName ()),
                        assembly.Location,
                        peImage: peImage));
                }
            }

            return configuration.With (
                evaluationContextId: evaluationContextId,
                globalStateType: globalStateTypeDefinition,
                initialReferences: assemblies);
        }

        public Task<IReadOnlyList<AssemblyLoadResult>> LoadAssembliesAsync (
            EvaluationContextId evaluationContextId,
            IReadOnlyList<AssemblyDefinition> assemblies,
            CancellationToken cancellationToken = default)
        {
            if (assemblies == null)
                assemblies = Array.Empty<AssemblyDefinition> ();

            var evaluationContext = GetEvaluationContext (evaluationContextId);

            var results = new AssemblyLoadResult [assemblies.Count];

            evaluationContext.AssemblyContext.AddRange (assemblies);

            for (var i = 0; i < assemblies.Count; i++) {
                var assembly = assemblies [i];
                var didLoad = false;
                var initializedIntegration = false;

                try {
                    Assembly loadedAssembly = null;

                    if (assembly.Content.Location.FileExists) {
                        loadedAssembly = Assembly.LoadFrom (assembly.Content.Location);
                        didLoad = true;
                    } else if (assembly.Content.PEImage != null) {
                        loadedAssembly = Assembly.Load (
                            assembly.Content.PEImage,
                            assembly.Content.DebugSymbols);
                        didLoad = true;
                    } else {
                        Log.Warning (
                            TAG,
                            $"Could not load assembly name {assembly.Name}, location didn't " +
                            "exist and PE image wasn't sent.");
                    }

                    if (loadedAssembly != null)
                        initializedIntegration = TryLoadIntegration (loadedAssembly);
                } catch (Exception e) {
                    Log.Error (TAG, $"Could not load sent assembly {assembly.Name}", e);
                } finally {
                    results [i] = new AssemblyLoadResult (
                        assembly.Name,
                        didLoad,
                        initializedIntegration);
                }
            }

            return Task.FromResult<IReadOnlyList<AssemblyLoadResult>> (results);
        }

        public Task EvaluateAsync (
            EvaluationContextId evaluationContextId,
            Compilation compilation,
            CancellationToken cancellationToken = default)
            => GetEvaluationContext (evaluationContextId).EvaluateAsync (
                compilation,
                cancellationToken);

        public Task AbortEvaluationAsync (
            EvaluationContextId evaluationContextId,
            CancellationToken cancellationToken = default)
        {
            GetEvaluationContext (evaluationContextId).AbortEvaluation ();
            return Task.CompletedTask;
        }
    }
}