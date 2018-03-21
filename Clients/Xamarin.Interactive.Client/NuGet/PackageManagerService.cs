// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Reflection;

using static Xamarin.Interactive.Compilation.InteractiveDependencyResolver;

namespace Xamarin.Interactive.NuGet
{
    sealed class PackageManagerService
    {
        const string TAG = nameof (PackageManagerService);

        // There are certain assemblies that we want to ban from being referenced because the explicit
        // references are not needed, and they make for Workbooks that aren't cross-platform in the
        // case of Xamarin.Forms.
        static readonly IReadOnlyList<string> bannedReferencePrefixes = new [] {
            "Xamarin.Forms.Platform.",
            "FormsViewGroup"
        };

        internal delegate Task<IAgentConnection> GetAgentConnectionHandler (
            bool refreshForAgentIntegration,
            CancellationToken cancellationToken);

        readonly IEvaluationService evaluationService;
        readonly DependencyResolver dependencyResolver;
        readonly GetAgentConnectionHandler getAgentConnectionHandler;

        InteractivePackageManager packageManager;

        public PackageManagerService (
            DependencyResolver dependencyResolver,
            IEvaluationService evaluationService,
            GetAgentConnectionHandler getAgentConnectionHandler)
        {
            this.evaluationService = evaluationService
                ?? throw new ArgumentNullException (nameof (evaluationService));

            this.dependencyResolver = dependencyResolver
                ?? throw new ArgumentNullException (nameof (dependencyResolver));

            this.getAgentConnectionHandler = getAgentConnectionHandler
                ?? throw new ArgumentNullException (nameof (getAgentConnectionHandler));
        }

        /// <summary>
        /// [Re]initializes all package management state. This should be invoked whenever
        /// any of <see cref="IWorkspaceService"/>, <see cref="IAgentConnection"/>, or
        /// <see cref="Sdk"/> for a given <see cref="Session.InteractiveSession"/> has changed.
        /// </summary>
        internal async Task InitializeAsync (
            Sdk targetSdk,
            IEnumerable<InteractivePackageDescription> initialPackages = null,
            CancellationToken cancellationToken = default)
        {
            if (targetSdk == null)
                throw new ArgumentNullException (nameof (targetSdk));

            var alreadyInstalledPackages = packageManager == null
                ? ImmutableArray<InteractivePackageDescription>.Empty
                : packageManager
                    .InstalledPackages
                    .Select (InteractivePackageDescription.FromInteractivePackage)
                    .ToImmutableArray ();

            packageManager = new InteractivePackageManager (
                targetSdk.TargetFramework,
                ClientApp
                    .SharedInstance
                    .Paths
                    .CacheDirectory
                    .Combine ("package-manager"));

            var packages = (initialPackages ?? Array.Empty<InteractivePackageDescription> ())
                .Concat (alreadyInstalledPackages)
                .Where (p => p.IsExplicitlySelected)
                .Distinct (PackageIdComparer.Default)
                .ToArray ();

            if (packages.Length == 0)
                return;

            await RestoreAsync (packages, cancellationToken);

            var agent = await getAgentConnectionHandler (false, cancellationToken);

            foreach (var package in packageManager.InstalledPackages)
                await LoadPackageIntegrationsAsync (agent, package, cancellationToken);
        }

        /// <summary>
        /// Install a set of NuGet packages and make them available for use in the <see cref="EvaluationService"/>.
        /// </summary>
        public async Task InstallAsync (
            IEnumerable<InteractivePackageDescription> packageDescriptions,
            CancellationToken cancellationToken = default)
        {
            if (packageDescriptions == null)
                throw new ArgumentNullException (nameof (packageDescriptions));

            foreach (var packageDescription in packageDescriptions) {
                await InstallAsync (packageDescription, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested ();
            }
        }

        /// <summary>
        /// Install a NuGet package and make it available for use in the <see cref="EvaluationService"/>.
        /// </summary>
        public async Task InstallAsync (
            InteractivePackageDescription packageDescription,
            CancellationToken cancellationToken = default)
        {
            if (packageDescription.PackageId == null)
                throw new ArgumentException (
                    $"{nameof (packageDescription.PackageId)} property cannot be null",
                    nameof (packageDescription));

            var package = packageDescription.ToInteractivePackage ();

            var installedPackages = await packageManager.InstallPackageAsync (
                package,
                packageDescription.GetSourceRepository (),
                cancellationToken);
            // TODO: Should probably alert user that the package is already installed.
            //       Should we add a fresh #r for the package in case that's what they're trying to get?
            //       A feel good thing?
            if (installedPackages.Count == 0)
                return;

            var agent = await getAgentConnectionHandler (false, cancellationToken);

            foreach (var installedPackage in installedPackages) {
                ReferencePackageInWorkspace (installedPackage);
                await LoadPackageIntegrationsAsync (agent, installedPackage, cancellationToken);
            }

            // TODO: Figure out metapackages. Install Microsoft.AspNet.SignalR, for example,
            //       and no #r submission gets generated, so all the workspace reference stuff
            //       above fails to bring in references to dependnet assemblies automatically.
            //       User must type them out themselves.
            //
            //       This was busted in our NuGet 2.x code as well.
            package = installedPackages.FirstOrDefault (
                p => PackageIdComparer.Equals (p, package));

            // TODO: Same issue as installedPackages.Count == 0. What do we want to tell user?
            //       probably they tried to install a package they already had installed, and
            //       maybe it bumped a shared dep (which is why installedPackages is non-empty).
            if (package == null)
                return;

            if (await ReferenceTopLevelPackageAsync (package, cancellationToken)) {
                evaluationService.OutdateAllCodeCells ();
                await evaluationService.EvaluateAllAsync (cancellationToken);
            }
        }

        public IReadOnlyList<InteractivePackageDescription> GetInstalledPackages ()
            => packageManager
                .InstalledPackages
                .Select (InteractivePackageDescription.FromInteractivePackage)
                .ToImmutableList ();

        public async Task RestoreAsync (
            IEnumerable<InteractivePackageDescription> packages,
            CancellationToken cancellationToken = default)
        {
            await packageManager.RestorePackagesAsync (
                packages.Select (package => package.ToInteractivePackage ()),
                cancellationToken);

            foreach (var package in packageManager.InstalledPackages) {
                ReferencePackageInWorkspace (package);
                await ReferenceTopLevelPackageAsync (package, cancellationToken);
            }
        }

        async Task LoadPackageIntegrationsAsync (
            IAgentConnection agent,
            InteractivePackage package,
            CancellationToken cancellationToken)
        {
            // Forms is special-cased because we own it and load the extension from our framework.
            if (PackageIdComparer.Equals (package.Identity.Id, "Xamarin.Forms")) {
                await WorkspaceConfiguration.LoadFormsAgentExtensions (
                    package.Identity.Version.Version,
                    agent,
                    dependencyResolver,
                    evaluationService.Id,
                    agent.IncludePeImage);
            }

            var assembliesToLoadOnAgent = new List<ResolvedAssembly> ();

            // Integration assemblies are not expected to be in a TFM directory—we look for them in
            // the `xamarin.interactive` folder inside the NuGet package.
            var packagePath = packageManager.GetPackageInstallPath (package);

            var interactivePath = packagePath.Combine ("xamarin.interactive");

            if (interactivePath.DirectoryExists) {
                var interactiveAssemblies = interactivePath.EnumerateFiles ("*.dll");
                foreach (var interactiveReference in interactiveAssemblies) {
                    var resolvedAssembly = dependencyResolver.ResolveWithoutReferences (interactiveReference);

                    if (HasIntegration (resolvedAssembly))
                        assembliesToLoadOnAgent.Add (resolvedAssembly);
                }
            }

            if (assembliesToLoadOnAgent.Count > 0) {
                var assembliesToLoad = assembliesToLoadOnAgent.Select (dep => {
                    var peImage = agent.IncludePeImage
                       ? GetFileBytes (dep.Path)
                       : null;
                    var syms = agent.IncludePeImage
                        ? GetDebugSymbolsFromAssemblyPath (dep.Path)
                        : null;
                    return new AssemblyDefinition (
                        dep.AssemblyName,
                        dep.Path,
                        peImage: peImage,
                        debugSymbols: syms
                    );
                }).ToArray ();

                await agent.Api.LoadAssembliesAsync (
                    evaluationService.Id,
                    assembliesToLoad);
            }

            await getAgentConnectionHandler (true, cancellationToken);
        }

        void ReferencePackageInWorkspace (InteractivePackage package)
        {
            foreach (var packageAssemblyReference in package.AssemblyReferences)
                dependencyResolver.AddAssemblySearchPath (
                    packageAssemblyReference.ParentDirectory);
        }

        async Task<bool> ReferenceTopLevelPackageAsync (
            InteractivePackage package,
            CancellationToken cancellationToken)
        {
            if (package.AssemblyReferences.Count == 0)
                return false;

            var references = new List<string> ();

            foreach (var packageAssemblyReference in package.AssemblyReferences) {
                var resolvedAssembly = dependencyResolver.ResolveWithoutReferences (packageAssemblyReference);
                if (resolvedAssembly == null)
                    continue;

                if (bannedReferencePrefixes.Any (resolvedAssembly.AssemblyName.Name.StartsWith))
                    continue;

                // Don't add #r for integration assemblies.
                if (HasIntegration (resolvedAssembly))
                    continue;

                references.Add (resolvedAssembly.AssemblyName.Name);
            }

            return await evaluationService.AddTopLevelReferencesAsync (references, cancellationToken);
        }

        bool HasIntegration (ResolvedAssembly resolvedAssembly)
        {
            try {
                var refAsm = Assembly.LoadFrom (resolvedAssembly.Path);

                if (refAsm == null)
                    return false;

                if (refAsm.GetReferencedAssemblies ().Any (r => r.Name == "Xamarin.Interactive")) {
                    var integrationType = refAsm
                        .GetCustomAttribute<AgentIntegrationAttribute> ()
                        ?.AgentIntegrationType;

                    if (integrationType != null)
                        return true;
                }

                return false;
            } catch (Exception e) {
                Log.Warning (TAG,
                     $"Couldn't load assembly {resolvedAssembly.AssemblyName.Name} for " +
                     $"agent integration loading",
                     e);
                return false;
            }
        }
    }
}