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
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Reflection;

using static Xamarin.Interactive.Compilation.InteractiveDependencyResolver;

namespace Xamarin.Interactive.Compilation
{
    static class CompilationWorkspaceFactory
    {
        const string TAG = nameof (CompilationWorkspaceFactory);

        public static async Task<RoslynCompilationWorkspace> CreateWorkspaceAsync (ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException (nameof (clientSession));

            if (!clientSession.Agent.IsConnected)
                throw new InvalidOperationException ("ClientSession must be connected to agent");

            var agentType = clientSession.Agent.Type;

            // HACK: This is a temporary fix to get iOS agent/app assemblies sent to the
            //       Windows client when using the remote sim.
            var includePeImage = clientSession.Agent.IncludePeImage;

            var configuration = await clientSession.Agent.Api.InitializeEvaluationContextAsync (includePeImage)
                .ConfigureAwait (false);
            if (configuration == null)
                return null;

            var dependencyResolver = CreateDependencyResolver (
                agentType,
                clientSession.Agent.AssemblySearchPaths);

            var defaultRefs = await clientSession.Agent.Api.GetAppDomainAssembliesAsync (includePeImage)
                .ConfigureAwait (false);

            // Only do this for Inspector sessions. Workbooks will do their own Forms init later.
            if (clientSession.SessionKind == ClientSessionKind.LiveInspection) {
                var formsReference = defaultRefs.FirstOrDefault (ra => ra.Name.Name == "Xamarin.Forms.Core");
                if (formsReference != null)
                    await LoadFormsAgentExtensions (
                        formsReference.Name.Version,
                        clientSession,
                        dependencyResolver,
                        configuration.EvaluationContextId,
                        includePeImage).ConfigureAwait (false);
            }

            dependencyResolver.AddDefaultReferences (defaultRefs);

            return await Task.Run (() => new RoslynCompilationWorkspace (
                dependencyResolver,
                configuration,
                agentType,
                ResolveHostObjectType (
                    dependencyResolver,
                    configuration,
                    agentType),
                includePeImage), clientSession.CancellationToken);
        }

        public static async Task LoadFormsAgentExtensions (
            Version formsVersion,
            ClientSession clientSession,
            DependencyResolver dependencyResolver,
            int evaluationContextId,
            bool includePeImage)
        {
            var formsAssembly = InteractiveInstallation.Default.LocateFormsAssembly (
                clientSession.Agent.Type);
            if (string.IsNullOrWhiteSpace (formsAssembly))
                return;

            var deps = dependencyResolver.Resolve (new [] { new FilePath (formsAssembly) });

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

            var res = await clientSession.Agent.Api.LoadAssembliesAsync (
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

        static InteractiveDependencyResolver CreateDependencyResolver (
            AgentType agentType,
            IEnumerable<string> assemblySearchPaths)
        {
            var dependencyResolver = new InteractiveDependencyResolver (agentType: agentType);
            var consoleOrWpf = agentType == AgentType.WPF || agentType == AgentType.Console;;

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
                } else
                    Log.Warning (TAG, $"Assembly search path {strPath} does not exist");
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
    }
}