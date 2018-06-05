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
using System.Text.RegularExpressions;
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

        public static Task<WorkspaceConfiguration> CreateAsync (
            IEvaluationContextManager evaluationContextManager,
            CancellationToken cancellationToken = default)
            => CreateAsync (
                evaluationContextManager,
                ClientSessionKind.Workbook,
                cancellationToken);

        internal static async Task<WorkspaceConfiguration> CreateAsync (
            IEvaluationContextManager evaluationContextManager,
            ClientSessionKind sessionKind,
            CancellationToken cancellationToken = default)
        {
            if (evaluationContextManager == null)
                throw new ArgumentNullException (nameof (evaluationContextManager));

            var configuration = await evaluationContextManager
                .CreateEvaluationContextAsync (cancellationToken)
                .ConfigureAwait (false);

            if (configuration == null)
                throw new Exception (
                    $"{nameof (IEvaluationContextManager)}." +
                    $"{nameof (IEvaluationContextManager.CreateEvaluationContextAsync)} " +
                    $"returned null");

            var dependencyResolver = CreateDependencyResolver (configuration);

            // Only do this for Inspector sessions. Workbooks will do their own Forms init later.
            if (sessionKind == ClientSessionKind.LiveInspection) {
                var formsReference = configuration
                    .InitialReferences
                    .FirstOrDefault (ra => ra.Name.Name == "Xamarin.Forms.Core");
                if (formsReference != null)
                    await LoadFormsAgentExtensions (
                        formsReference.Name.Version,
                        configuration,
                        evaluationContextManager,
                        dependencyResolver).ConfigureAwait (false);
            }

            dependencyResolver.AddDefaultReferences (configuration.InitialReferences);

            var globalStateType = ResolveHostObjectType (
                dependencyResolver,
                configuration);

            configuration = configuration.With (globalStateType: configuration
                .GlobalStateType
                .WithResolvedType (globalStateType));

            return new WorkspaceConfiguration (configuration, dependencyResolver);
        }

        static InteractiveDependencyResolver CreateDependencyResolver (
            TargetCompilationConfiguration configuration)
        {
            var dependencyResolver = new InteractiveDependencyResolver (configuration);
            var scanRecursively = configuration.Sdk?.TargetFramework.Identifier == ".NETFramework";

            foreach (FilePath path in configuration.AssemblySearchPaths) {
                if (path.DirectoryExists) {
                    Log.Info (TAG, $"Searching assembly path {path} (recursive: {scanRecursively})");
                    dependencyResolver.AddAssemblySearchPath (
                        path,
                        scanRecursively);

                    if (!scanRecursively) {
                        var facadesPath = path.Combine ("Facades");
                        if (facadesPath.DirectoryExists) {
                            Log.Info (TAG, $"Searching assembly path {facadesPath}");
                            dependencyResolver.AddAssemblySearchPath (facadesPath);
                        }
                    }
                } else {
                    Log.Warning (TAG, $"Assembly search path {path} does not exist");
                }
            }

            return dependencyResolver;
        }

        static Assembly netStandardAssembly;
        static Assembly xiAssembly;

        static readonly Regex dncGlobalStateTypeHackRegex = new Regex (
            @"^Xamarin\.Interactive\..*EvaluationContextGlobalObject$");

        static Type ResolveHostObjectType (
            InteractiveDependencyResolver dependencyResolver,
            TargetCompilationConfiguration configuration)
        {
            if (configuration.GlobalStateType?.Name == null)
                return typeof (object);

            if (System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith (
                ".NET Core", StringComparison.OrdinalIgnoreCase)) {
                // .NET Core does not support reflection-only-load and due to a bad design decision
                // in Roslyn's scripting support, we cannot pass SRM or Roslyn type symbols to
                // the compiler configuration for it to know the global API shape.
                // cf. https://github.com/dotnet/corefx/issues/2800
                // cf. https://github.com/dotnet/roslyn/issues/20920
                //
                // Employ a hack here to get our base globals type working by checking the name
                // of the type and assuming it is or dervives from EvaluationContextGlobalObject.
                //
                // Unfortunately this still makes agent-specific API such as "MainWindow" unavailable
                // in the Web UX (since Roslyn is running under ASP.NET Core).
                if (dncGlobalStateTypeHackRegex.IsMatch (configuration.GlobalStateType.Name))
                    return typeof (EvaluationContextGlobalObject);

                return typeof (object);
            }

            using (var assemblyContext = new NetFxEvaluationAssemblyContext ()) {
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
            TargetCompilationConfiguration configuration,
            IEvaluationContextManager evaluationContextManager,
            DependencyResolver dependencyResolver)
        {
            var formsAssembly = InteractiveInstallation.Default.LocateFormsAssembly (configuration.Sdk?.Id);
            if (string.IsNullOrWhiteSpace (formsAssembly))
                return;

            var deps = dependencyResolver.Resolve (new [] { new FilePath (formsAssembly) });
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