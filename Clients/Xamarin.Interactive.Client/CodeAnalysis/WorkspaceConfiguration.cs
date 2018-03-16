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
using Xamarin.Interactive.Compilation;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Reflection;

using static Xamarin.Interactive.Compilation.InteractiveDependencyResolver;

namespace Xamarin.Interactive.CodeAnalysis
{
    sealed class WorkspaceConfiguration
    {
        const string TAG = nameof (WorkspaceConfiguration);

        public TargetCompilationConfiguration CompilationConfiguration { get; }
        public InteractiveDependencyResolver DependencyResolver { get; }
        public bool IncludePEImagesInDependencyResolution { get; }
        public Type HostObjectType { get; }

        public WorkspaceConfiguration (
            TargetCompilationConfiguration compilationConfiguration,
            InteractiveDependencyResolver dependencyResolver,
            bool includePEImagesInDependencyResolution,
            Type hostObjectType = null)
        {
            CompilationConfiguration = compilationConfiguration
                ?? throw new ArgumentNullException (nameof (compilationConfiguration));

            DependencyResolver = dependencyResolver
                ?? throw new ArgumentNullException (nameof (dependencyResolver));

            IncludePEImagesInDependencyResolution = includePEImagesInDependencyResolution;
            HostObjectType = hostObjectType;
        }

        public static async Task<WorkspaceConfiguration> CreateAsync (
            IAgentConnection agent,
            ClientSessionKind sessionKind,
            CancellationToken cancellationToken = default)
        {
            // HACK: This is a temporary fix to get iOS agent/app assemblies
            // sent to the Windows client when using the remote simulator.
            var includePeImage = agent.IncludePeImage;

            var configuration = await agent.Api.InitializeEvaluationContextAsync (includePeImage)
                .ConfigureAwait (false);
            if (configuration == null)
                throw new Exception (
                    $"{nameof (agent.Api.InitializeEvaluationContextAsync)} " +
                    $"did not return a {nameof (TargetCompilationConfiguration)}");

            var dependencyResolver = CreateDependencyResolver (
                agent.Type,
                agent.AssemblySearchPaths);

            var defaultRefs = await agent.Api.GetAppDomainAssembliesAsync (includePeImage)
                .ConfigureAwait (false);

            // Only do this for Inspector sessions. Workbooks will do their own Forms init later.
            if (sessionKind == ClientSessionKind.LiveInspection) {
                var formsReference = defaultRefs.FirstOrDefault (ra => ra.Name.Name == "Xamarin.Forms.Core");
                if (formsReference != null)
                    await LoadFormsAgentExtensions (
                        formsReference.Name.Version,
                        agent,
                        dependencyResolver,
                        configuration.EvaluationContextId,
                        includePeImage).ConfigureAwait (false);
            }

            dependencyResolver.AddDefaultReferences (defaultRefs);

            return new WorkspaceConfiguration (
                configuration,
                dependencyResolver,
                includePeImage,
                ResolveHostObjectType (
                    dependencyResolver,
                    configuration,
                    agent.Type));
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
                if (configuration.GlobalStateAssembly.Content.PEImage != null)
                    globalStateAssemblyCachePath =
                        dependencyResolver.CacheRemoteAssembly (
                            configuration.GlobalStateAssembly);

                var resolvedAssemblies = dependencyResolver
                    .Resolve (new [] { configuration.GlobalStateAssembly })
                    .Select (r => new AssemblyDefinition (r.AssemblyName, r.Path));

                assemblyContext.AddRange (resolvedAssemblies);

                var globalStateAssemblyDef = resolvedAssemblies.First (
                    assembly => ResolvedAssembly.NameEqualityComparer.Default.Equals (
                        assembly.Name,
                        configuration.GlobalStateAssembly.Name));

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

                return globalStateAssembly.GetType (configuration.GlobalStateTypeName);
            }
        }

        internal static async Task LoadFormsAgentExtensions (
            Version formsVersion,
            IAgentConnection agent,
            DependencyResolver dependencyResolver,
            EvaluationContextId evaluationContextId,
            bool includePeImage)
        {
            var formsAssembly = InteractiveInstallation.Default.LocateFormsAssembly (agent.Type);
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

            var res = await agent.Api.LoadAssembliesAsync (
                evaluationContextId,
                assembliesToLoad);

            var failed = res.LoadResults.Where (p => !p.Success);
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