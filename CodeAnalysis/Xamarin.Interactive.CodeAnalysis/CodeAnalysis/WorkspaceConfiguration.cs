//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

using static Xamarin.Interactive.CodeAnalysis.Resolving.InteractiveDependencyResolver;

namespace Xamarin.Interactive.CodeAnalysis
{
    public sealed class WorkspaceConfiguration
    {
        const string TAG = nameof (WorkspaceConfiguration);

        public TargetCompilationConfiguration CompilationConfiguration { get; }
        public InteractiveDependencyResolver DependencyResolver { get; }

        public WorkspaceConfiguration (
            TargetCompilationConfiguration compilationConfiguration,
            InteractiveDependencyResolver dependencyResolver)
        {
            CompilationConfiguration = compilationConfiguration;
            DependencyResolver = dependencyResolver;
        }

        internal static async Task<WorkspaceConfiguration> CreateAsync (
            AgentType agentType,
            IEvaluationContextManager evaluationContextManager,
            ClientSessionKind sessionKind,
            CancellationToken cancellationToken = default)
        {
            var configuration = await evaluationContextManager
                .CreateEvaluationContextAsync (cancellationToken)
                .ConfigureAwait (false);

            if (configuration == null)
                throw new Exception (
                    $"{nameof (IEvaluationContextManager)}." +
                    $"{nameof (IEvaluationContextManager.CreateEvaluationContextAsync)} " +
                    $"returned null");

            var dependencyResolver = CreateDependencyResolver (
                agentType,
                configuration.AssemblySearchPaths);

            // Only do this for Inspector sessions. Workbooks will do their own Forms init later.
            if (sessionKind == ClientSessionKind.LiveInspection) {
                var formsReference = configuration
                    .InitialReferences
                    .FirstOrDefault (ra => ra.Name.Name == "Xamarin.Forms.Core");
                if (formsReference != null)
                    await LoadFormsAgentExtensions (
                        formsReference.Name.Version,
                        agentType,
                        configuration,
                        evaluationContextManager,
                        dependencyResolver).ConfigureAwait (false);
            }

            dependencyResolver.AddDefaultReferences (configuration.InitialReferences);

            var globalStateType = ResolveHostObjectType (
                dependencyResolver,
                configuration,
                agentType);

            configuration = configuration.With (globalStateType: configuration
                .GlobalStateType
                .WithResolvedType (globalStateType));

            return new WorkspaceConfiguration (configuration, dependencyResolver);
        }

        static InteractiveDependencyResolver CreateDependencyResolver (
            AgentType agentType,
            IEnumerable<string> assemblySearchPaths)
        {
            var dependencyResolver = new InteractiveDependencyResolver (agentType: agentType);
            var consoleOrWpf = agentType == AgentType.WPF || agentType == AgentType.Console;

            foreach (var strPath in assemblySearchPaths) {
                var path = new FilePath (strPath);
                if (path.DirectoryExists) {
                    Log.Info (TAG, $"Searching assembly path {path}");
                    dependencyResolver.AddAssemblySearchPath (
                        path,
                        scanRecursively: consoleOrWpf);

                    if (!consoleOrWpf) {
                        path = path.Combine ("Facades");
                        if (path.DirectoryExists)
                            dependencyResolver.AddAssemblySearchPath (path);
                    }
                } else {
                    Log.Warning (TAG, $"Assembly search path {strPath} does not exist");
                }
            }

            return dependencyResolver;
        }

        static Assembly netStandardAssembly;
        static Assembly xiAssembly;

        static Type ResolveHostObjectType (
            InteractiveDependencyResolver dependencyResolver,
            TargetCompilationConfiguration configuration,
            AgentType agentType)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith (
                ".NET Core", StringComparison.OrdinalIgnoreCase))
                return typeof (object);

            using (var assemblyContext = new EvaluationAssemblyContext ()) {
                string globalStateAssemblyCachePath = null;
                if (configuration.GlobalStateType.Assembly.Content.PEImage != null)
                    globalStateAssemblyCachePath =
                        dependencyResolver.CacheRemoteAssembly (
                            configuration.GlobalStateType.Assembly);

                var resolvedAssemblies = dependencyResolver
                    .Resolve (new [] { configuration.GlobalStateType.Assembly })
                    .Select (r => new AssemblyDefinition (r.AssemblyName, r.Path));

                assemblyContext.AddRange (resolvedAssemblies);

                var globalStateAssemblyDef = resolvedAssemblies.First (
                    assembly => ResolvedAssembly.NameEqualityComparer.Default.Equals (
                        assembly.Name,
                        configuration.GlobalStateType.Assembly.Name));

                netStandardAssembly = netStandardAssembly ??
                    Assembly.ReflectionOnlyLoadFrom (
                        new FilePath (Assembly.GetExecutingAssembly ().Location)
                            .ParentDirectory
                            .Combine ("netstandard.dll"));

                xiAssembly = xiAssembly ??
                    Assembly.ReflectionOnlyLoadFrom (
                        new FilePath (Assembly.GetExecutingAssembly ().Location)
                            .ParentDirectory
                            .Combine ("Xamarin.Interactive.dll"));

                Assembly globalStateAssembly;
                if (globalStateAssemblyDef.Name.Name == "Xamarin.Interactive")
                    globalStateAssembly = xiAssembly;
                else
                    globalStateAssembly = Assembly.ReflectionOnlyLoadFrom (
                        globalStateAssemblyCachePath ?? globalStateAssemblyDef.Content.Location);

                return globalStateAssembly.GetType (configuration.GlobalStateType.Name);
            }
        }

        internal static async Task LoadFormsAgentExtensions (
            Version formsVersion,
            AgentType agentType,
            TargetCompilationConfiguration configuration,
            IEvaluationContextManager evaluationContextManager,
            DependencyResolver dependencyResolver)
        {
            var formsAssembly = InteractiveInstallation.Default.LocateFormsAssembly (agentType);
            if (string.IsNullOrWhiteSpace (formsAssembly))
                return;

            var deps = dependencyResolver.Resolve (new [] { new FilePath (formsAssembly) });

            // Now dig out the resolved assembly that is Xamarin.Forms.Core, and compare it to the
            // default ref.
            var resolvedFormsAssembly = deps.FirstOrDefault (d => d.AssemblyName.Name == "Xamarin.Forms.Core");
            if (resolvedFormsAssembly == null) {
                Log.Warning (TAG,
                    "Cannot enable Forms integration because Forms cannot be " +
                    "resolved. Check log for assembly search path issues.");
                return;
            }
            var ourVersion = resolvedFormsAssembly.AssemblyName.Version;
            var theirVersion = formsVersion;

            if (ourVersion.Major != theirVersion.Major) {
                Log.Warning (
                    TAG,
                    "Assembly version mismatch between app's Xamarin.Forms.Core and" +
                    $"our referenced Xamarin.Forms.Core. Our version: {ourVersion}, " +
                    $"their version: {theirVersion}. Won't load Xamarin.Forms agent " +
                    "integration, as it probably won't work."
                );
                return;
            }

            var includePeImage = configuration.IncludePEImagesInDependencyResolution;
            var assembliesToLoad = deps.Select (dep => {
                var peImage = includePeImage ? GetFileBytes (dep.Path) : null;
                var syms = includePeImage ? GetDebugSymbolsFromAssemblyPath (dep.Path) : null;
                return new AssemblyDefinition (
                    dep.AssemblyName,
                    dep.Path,
                    peImage: peImage,
                    debugSymbols: syms
                );
            }).ToArray ();

            var results = await evaluationContextManager.LoadAssembliesAsync (
                configuration.EvaluationContextId,
                assembliesToLoad);

            var failed = results.Where (p => !p.Success);
            if (failed.Any ()) {
                var failedLoads = string.Join (", ", failed.Select (p => p.AssemblyName.Name));
                Log.Warning (
                    TAG,
                    $"Xamarin.Forms reference detected, but integration may not have" +
                    $" loaded properly. Assemblies that did not load: {failedLoads}");
            }
        }
    }
}