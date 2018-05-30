// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

using static Xamarin.Interactive.CodeAnalysis.Resolving.InteractiveDependencyResolver;

namespace Xamarin.Interactive.NuGet
{
    public sealed class PackageManagerService : INotifyPropertyChanged
    {
        const string TAG = nameof (PackageManagerService);

        // There are certain assemblies that we want to ban from being referenced because the explicit
        // references are not needed, and they make for Workbooks that aren't cross-platform in the
        // case of Xamarin.Forms.
        static readonly IReadOnlyList<string> bannedReferencePrefixes = new [] {
            "Xamarin.Forms.Platform.",
            "FormsViewGroup"
        };

        InteractivePackageManager packageManager;

        PackageReferenceList packageReferences = PackageReferenceList.Empty;
        public PackageReferenceList PackageReferences {
            get => packageReferences;
            set {
                if (packageReferences != value) {
                    packageReferences = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        IReadOnlyList<InteractivePackage> installedPackages;
        internal IReadOnlyList<InteractivePackage> InstalledPackages {
            get => installedPackages;
            set {
                if (installedPackages != value) {
                    installedPackages = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        /// <summary>
        /// [Re]initializes all package management state. This should be invoked whenever
        /// any of <see cref="IWorkspaceService"/>, <see cref="IAgentConnection"/>, or
        /// <see cref="Sdk"/> for a given <see cref="Session.InteractiveSession"/> has changed.
        /// </summary>
        public Task InitializeAsync (
            string runtimeIdentifier,
            FrameworkName targetFramework,
            FilePath packageConfigDirectory,
            IEnumerable<InteractivePackageDescription> initialPackageReferences = null,
            CancellationToken cancellationToken = default)
        {
            if (targetFramework == null)
                throw new ArgumentNullException (nameof (targetFramework));

            packageManager = new InteractivePackageManager (
                runtimeIdentifier,
                targetFramework,
                packageConfigDirectory);

            return RestoreAsync (
                initialPackageReferences,
                cancellationToken);
        }

        public Task InstallAsync (
            InteractivePackageDescription packageReference,
            CancellationToken cancellationToken = default)
            => RestoreAsync (
                this.packageReferences.AddOrUpdate (packageReference),
                cancellationToken);

        public Task InstallAsync (
            IEnumerable<InteractivePackageDescription> packageReferences,
            CancellationToken cancellationToken = default)
            => RestoreAsync (
                this.packageReferences.AddOrUpdate (packageReferences
                    ?? Enumerable.Empty<InteractivePackageDescription> ()),
                cancellationToken);

        public Task RestoreAsync (
            IEnumerable<InteractivePackageDescription> packageReferences,
            CancellationToken cancellationToken = default)
            => RestoreAsync (
                this.packageReferences.ReplaceAllWith (packageReferences
                    ?? Enumerable.Empty<InteractivePackageDescription> ()),
                cancellationToken);

        async Task RestoreAsync (
            PackageReferenceList packageReferences,
            CancellationToken cancellationToken = default)
        {
            if (PackageReferences != packageReferences) {
                PackageReferences = packageReferences;
                InstalledPackages = await packageManager.RestoreAsync (
                    packageReferences,
                    cancellationToken).ConfigureAwait (false);
            }
        }

        #if false

        async Task LoadPackageIntegrationsAsync (
            AgentType agentType,
            TargetCompilationConfiguration targetCompilationConfiguration,
            IEvaluationContextManager evaluationContextManager,
            InteractivePackage package,
            CancellationToken cancellationToken)
        {
            // Forms is special-cased because we own it and load the extension from our framework.
            if (PackageIdComparer.Equals (package.Identity.Id, "Xamarin.Forms"))
                await WorkspaceConfiguration.LoadFormsAgentExtensions (
                    package.Identity.Version.Version,
                    targetCompilationConfiguration,
                    evaluationContextManager,
                    dependencyResolver);

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
                var includePeImage = targetCompilationConfiguration.IncludePEImagesInDependencyResolution;

                var assembliesToLoad = assembliesToLoadOnAgent.Select (dep => {
                    var peImage = includePeImage
                       ? GetFileBytes (dep.Path)
                       : null;
                    var syms = includePeImage
                        ? GetDebugSymbolsFromAssemblyPath (dep.Path)
                        : null;
                    return new AssemblyDefinition (
                        dep.AssemblyName,
                        dep.Path,
                        peImage: peImage,
                        debugSymbols: syms
                    );
                }).ToArray ();

                await evaluationContextManager.LoadAssembliesAsync (
                    targetCompilationConfiguration.EvaluationContextId,
                    assembliesToLoad,
                    cancellationToken);
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
                        .GetCustomAttribute<EvaluationContextManagerIntegrationAttribute> ()
                        ?.IntegrationType;

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

        #endif
    }
}