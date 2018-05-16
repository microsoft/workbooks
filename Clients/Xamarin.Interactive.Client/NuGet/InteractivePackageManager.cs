//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.NuGet
{
    sealed class InteractivePackageManager : INotifyPropertyChanged
    {
        const string TAG = nameof (InteractivePackageManager);

        internal static readonly PackageIdentity FixedXamarinFormsPackageIdentity = new PackageIdentity (
            "Xamarin.Forms",
            new NuGetVersion (3, 0, 0, 482510));

        readonly NuGetPackageManager packageManager;
        readonly InteractivePackageProjectContext projectContext;
        readonly InteractiveNuGetProject project;
        readonly ISettings settings;
        readonly FilePath packageConfigDirectory;

        ImmutableHashSet<InteractivePackage> installedPackages = ImmutableHashSet<InteractivePackage>
            .Empty
            .WithComparer (PackageIdComparer.Default);

        ImmutableArray<InteractivePackage> sortedInstalledPackages = ImmutableArray<InteractivePackage>.Empty;

        public NuGetFramework TargetFramework { get; }
        public ImmutableArray<SourceRepository> SourceRepositories { get; }
        public ILogger Logger { get; }

        public ImmutableArray<InteractivePackage> InstalledPackages => sortedInstalledPackages;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <param name="packageConfigDirectory">A directory where the package manager can store temporary
        /// files, and where nuget.config settings files could be placed specific to Workbooks.</param>
        public InteractivePackageManager (
            FrameworkName targetFramework,
            FilePath packageConfigDirectory)
        {
            if (targetFramework == null)
                throw new ArgumentNullException (nameof (targetFramework));
            if (packageConfigDirectory.IsNull)
                throw new ArgumentNullException (nameof (packageConfigDirectory));

            TargetFramework = NuGetFramework.ParseFrameworkName (
                targetFramework.FullName,
                DefaultFrameworkNameProvider.Instance);
            this.packageConfigDirectory = packageConfigDirectory;

            Logger = new InteractivePackageLogger ();

            var primarySourceUrl = NuGetConstants.V3FeedUrl;
            settings = Settings.LoadDefaultSettings (packageConfigDirectory);
            var packageSourceProvider = new PackageSourceProvider (settings);
            var configuredPackageSources = packageSourceProvider.LoadPackageSources ();
            var v3Providers = Repository.Provider.GetCoreV3 ();

            void LogPackageSource (PackageSource source, bool isPrimary = false, bool isIgnored = false)
                => Log.Info (
                    TAG,
                    $"    [{source.Name}] {source.Source}" +
                    (source.IsEnabled ? "" : " (disabled)") +
                    (isIgnored ? " (ignored)" : "") +
                    (isPrimary ? " (primary)" : ""));

            Log.Info (TAG, "Enumerating NuGet source repositories");

            SourceRepository primarySource = null;
            var sourcesBuilder = ImmutableArray.CreateBuilder<SourceRepository> ();

            foreach (var packageSource in configuredPackageSources) {
                if (!packageSource.IsEnabled) {
                    LogPackageSource (packageSource);
                    continue;
                }

                var sourceRepo = new SourceRepository (packageSource, v3Providers);
                var sourceHost = packageSource.TrySourceAsUri?.Host;

                // Ignore nuget.org sources that are not the expected v3 source
                if (string.Equals (sourceHost, "nuget.org", StringComparison.OrdinalIgnoreCase) ||
                    sourceHost.EndsWith (".nuget.org", StringComparison.OrdinalIgnoreCase)) {
                    if (packageSource.Source == primarySourceUrl)
                        primarySource = sourceRepo;
                    else
                        LogPackageSource (packageSource, isIgnored: true);
                } else {
                    sourcesBuilder.Add (sourceRepo);
                    LogPackageSource (packageSource);
                }
            }

            if (primarySource == null)
                primarySource = new SourceRepository (
                    new PackageSource (primarySourceUrl, "nuget.org"),
                    v3Providers);
            LogPackageSource (primarySource.PackageSource, isPrimary: true);
            sourcesBuilder.Insert (0, primarySource);

            SourceRepositories = sourcesBuilder.ToImmutable ();

            // NOTE: NuGetPackageManager requires a directory, even though ultimately it is not used
            //       by the methods we call.
            packageManager = new NuGetPackageManager (
                new SourceRepositoryProvider (packageSourceProvider, v3Providers),
                settings,
                packageConfigDirectory);
            projectContext = new InteractivePackageProjectContext (Logger);
            project = new InteractiveNuGetProject (TargetFramework, settings);
        }

        void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        void UpdateInstalledPackages ()
        {
            sortedInstalledPackages = installedPackages
                .OrderBy (p => p.Identity.Id)
                .ToImmutableArray ();

            MainThread.Post (() => NotifyPropertyChanged (nameof (InstalledPackages)));
        }

        public void RemovePackage (InteractivePackage package)
        {
            if (package == null)
                return;

            installedPackages = installedPackages.Remove (package);

            UpdateInstalledPackages ();
        }

        public FilePath GetPackageInstallPath (InteractivePackage package)
            => project.GetInstalledPath (package.Identity);

        /// <summary>
        /// Install a NuGet package. Returns all newly installed packages.
        /// </summary>
        public async Task<IReadOnlyCollection<InteractivePackage>> InstallPackageAsync (
            InteractivePackage package,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            if (package == null)
                throw new ArgumentNullException (nameof (package));
            if (!package.Identity.HasVersion)
                throw new ArgumentException ("PackageIdentity.Version must be set");

            // TODO: File upstream issue about exception if primary source repo is offline.
            //       Shouldn't secondary source repos kick in? Our current work around is to
            //       pass the source repo from search to install, but that's not perfect.
            sourceRepository = sourceRepository ?? SourceRepositories [0];

            project.ResetInstallationContext ();

            // Just need to apply one fixup here
            if (PackageIdComparer.Equals (package.Identity.Id, FixedXamarinFormsPackageIdentity.Id) &&
                package.Identity.Version != FixedXamarinFormsPackageIdentity.Version) {
                Log.Warning (
                    TAG,
                    $"Replacing requested Xamarin.Forms version {package.Identity.Version} with " +
                    $"required version {FixedXamarinFormsPackageIdentity.Version}.");
                package = package.WithVersion (
                    FixedXamarinFormsPackageIdentity.Version,
                    overwriteRange: true);
            }

            const string integrationPackage = PackageManagerViewModel.IntegrationPackageId;
            if (PackageIdComparer.Equals (package.Identity.Id, integrationPackage)) {
                Log.Warning (TAG, $"Refusing to add integration NuGet package {integrationPackage}.");
                return Array.Empty<InteractivePackage> ();
            }

            var resolutionContext = new ResolutionContext (
                DependencyBehavior.Lowest, // IDEs only use Highest if upgrading
                includePrelease: true,
                includeUnlisted: true,
                versionConstraints: VersionConstraints.None);

            // Although there is a single repo associated with the package being installed,
            // dependency resolution will also look into the secondary sources. In some cases,
            // this can greatly slow down installation. For the primary case of searching for
            // packages in nuget.org, prevent the package manager from using secondary sources
            // for resolution.
            //
            // It is important to pass an empty enumerable, because if we pass null, the package
            // manager will determine secondary sources based on the NuGet configuration.
            var secondarySources =
                sourceRepository == SourceRepositories [0]
                ? Enumerable.Empty<SourceRepository> ()
                : SourceRepositories.Where (r => r != sourceRepository).ToArray ();

            // There does not appear to be a way to hook into or override functionality of the
            // NuGetPackageManager or PackageResolver classes. In order to mess with package
            // resolution, we need to either write a lot of code, proxy the sources, or intercede
            // via preview installation actions.
            //
            // Here we do the latter, though it is not the best general-purpose approach. It works
            // fine for replacing one single package that we know a LOT about. If that package's
            // dependencies continually changed, we'd be better off with another approach.
            var previewInstallActions = await packageManager.PreviewInstallPackageAsync (
                project,
                package.Identity,
                resolutionContext,
                projectContext,
                sourceRepository,
                secondarySources,
                cancellationToken);

            var installActions = new List<NuGetProjectAction> ();
            foreach (var action in previewInstallActions) {
                // If the installed package has a dependency on Xamarin.Forms, make sure the version
                // that gets installed is our preferred version. Force it to install from the primary
                // source repository, because we can't assume that version is available everywhere.
                //
                // TODO: Consider adding a search or something to see if we can use the specified source
                //       instead. Could be handy if nuget.org is down or the user is offline and using
                //       a local repo.
                if (action.PackageIdentity.Id == FixedXamarinFormsPackageIdentity.Id)
                    installActions.Add (NuGetProjectAction.CreateInstallProjectAction (
                        FixedXamarinFormsPackageIdentity,
                        SourceRepositories [0],
                        action.Project));
                else
                    installActions.Add (action);
            }

            // We follow the modern behavior of .NET Core and do not actually install packages anywhere.
            // Instead, we ultimately reference them out of the user's global package cache (by default,
            // ~/.nuget/packages). Our NuGetProject implementation simply collects package assembly
            // references (and potentially other necessary files) and populates them back into the
            // InteractiveInstallationContext.
            await packageManager.ExecuteNuGetProjectActionsAsync (
                project,
                installActions,
                projectContext,
                cancellationToken);

            // Identify which packages were not already noted as installed, or have been upgraded now
            var newlyInstalledPackages = new List<InteractivePackage> ();
            foreach (var newPackage in project.InstallationContext.InstalledPackages) {
                InteractivePackage finalNewPackage;
                var foundInstalledMatch = installedPackages.TryGetValue (
                    newPackage,
                    out finalNewPackage);

                if (!foundInstalledMatch ||
                    newPackage.Identity.Version > finalNewPackage.Identity.Version) {

                    // Make sure we have a reference to a matching explicit InteractivePackage if it
                    // exists, so that we can persist the original SupportedVersionRange
                    if (!foundInstalledMatch)
                        finalNewPackage = PackageIdComparer.Equals (package, newPackage)
                            ? package
                            : newPackage;

                    finalNewPackage = newPackage
                        .WithIsExplicit (finalNewPackage.IsExplicit)
                        .WithSupportedVersionRange (finalNewPackage.SupportedVersionRange);

                    newlyInstalledPackages.Add (finalNewPackage);
                    installedPackages = installedPackages
                        .Remove (finalNewPackage)
                        .Add (finalNewPackage);
                    UpdateInstalledPackages ();
                }
            }

            return newlyInstalledPackages;
        }

        public Task RestorePackagesAsync (
            IEnumerable<InteractivePackage> packages,
            CancellationToken cancellationToken)
        {
            using (var cacheContext = new SourceCacheContext ()) {
                return RestorePackagesAsync (
                    packages,
                    cacheContext,
                    cancellationToken);
            }
        }

        async Task RestorePackagesAsync (
            IEnumerable<InteractivePackage> packages,
            SourceCacheContext cacheContext,
            CancellationToken cancellationToken)
        {
            var restoreContext = new RestoreArgs {
                CacheContext = cacheContext,
                Log = Logger,
            };

            // NOTE: This path is typically empty. It could in theory contain nuget.config settings
            //       files, but really we just use it to satisfy nuget API that requires paths,
            //       even when they are never used.
            var rootPath = packageConfigDirectory;
            var globalPath = restoreContext.GetEffectiveGlobalPackagesFolder (rootPath, settings);
            var fallbackPaths = restoreContext.GetEffectiveFallbackPackageFolders (settings);

            var providerCache = new RestoreCommandProvidersCache ();
            var restoreProviders = providerCache.GetOrCreate (
                globalPath,
                fallbackPaths,
                SourceRepositories,
                cacheContext,
                Logger);

            // Set up a project spec similar to what you would see in a project.json.
            // This is sufficient for the dependency graph work done within RestoreCommand.
            // TODO: XF version pinning during restore?
            var targetFrameworkInformation = new TargetFrameworkInformation {
                FrameworkName = TargetFramework,
                Dependencies = packages.Select (ToLibraryDependency).ToList (),
            };
            var projectSpec = new PackageSpec (new [] { targetFrameworkInformation }) {
                Name = project.Name,
                FilePath = rootPath,
            };

            var restoreRequest = new RestoreRequest (projectSpec, restoreProviders, cacheContext, Logger);
            var restoreCommand = new RestoreCommand (restoreRequest);
            var result = await restoreCommand.ExecuteAsync (cancellationToken);

            if (!result.Success)
                return;

            project.ResetInstallationContext ();

            // As with installation, restore simply ensures that packages are present in the user's
            // global package cache. We reference them out of there just like .NET core projects do.
            //
            // All resolved packages, including the explicit inputs and their dependencies, are
            // available as LockFileLibrary instances.
            foreach (var library in result.LockFile.Libraries)
                project.InstallationContext.AddInstalledPackage (
                    GetInteractivePackageFromLibrary (library, project, packages));

            installedPackages = project.InstallationContext.InstalledPackages;
            UpdateInstalledPackages ();
        }

        /// <summary>
        /// Get an InteractivePackage for a restored NuGet package, populated with assembly references from the
        /// user's global package cache.
        /// </summary>
        /// <returns>An InteractivePackage with populated assembly references, or null if the LockFileLibrary
        /// is not of type "package".</returns>
        /// <param name="inputPackages">Input to RestorePackagesAsync, used to ensure the returned package
        /// has the same SupportedVersionRange as the requested package and to determine if this is a
        /// user-specified package or a dependency.</param>
        static InteractivePackage GetInteractivePackageFromLibrary (
            LockFileLibrary library,
            InteractiveNuGetProject project,
            IEnumerable<InteractivePackage> inputPackages)
        {
            if (library.Type != "package") // TODO: where does this string come from?
                return null;

            var packageIdentity = new PackageIdentity (library.Name, library.Version);

            // All package files are listed within result.LockFile.Libraries[i].Files, but there are no
            // utilities to pick out framework-specific files. Using package readers probably slows
            // restore down but for now it's less code and more consistency.
            using (var packageReader = new PackageFolderReader (project.GetInstalledPath (packageIdentity)))
                return GetInteractivePackageFromReader (packageReader, project, inputPackages);
        }

        /// <summary>
        /// Get an InteractivePackage with populated assembly references from a restored NuGet package.
        /// </summary>
        /// <param name="inputPackages">Input to RestorePackagesAsync, used to ensure the returned package
        /// has the same SupportedVersionRange as the requested package, and to determine if this is a
        /// user-specified package or a dependency.</param>
        static InteractivePackage GetInteractivePackageFromReader (
            PackageReaderBase packageReader,
            InteractiveNuGetProject project,
            IEnumerable<InteractivePackage> inputPackages)
        {
            ImmutableList<FilePath> assemblyReferences = null;
            var fx = project.TargetFramework;
            var packageIdentity = packageReader.GetIdentity ();

            if (packageReader
                .GetSupportedFrameworks ()
                .Any (f => DefaultCompatibilityProvider.Instance.IsCompatible (fx, f)))
                assemblyReferences =
                    project.GetPackageAssemblyReferences (packageReader, packageIdentity);

            var originalInputPackage = inputPackages.FirstOrDefault (
                p => PackageIdComparer.Equals (p.Identity, packageIdentity));

            // Persist original VersionRange to what gets in the installed package list so that
            // the same original version range string gets written to the manifest on save
            return new InteractivePackage (
                packageIdentity,
                isExplicit: originalInputPackage?.IsExplicit == true,
                assemblyReferences: assemblyReferences,
                supportedVersionRange: originalInputPackage?.SupportedVersionRange);
        }

        static LibraryDependency ToLibraryDependency (InteractivePackage package)
            => new LibraryDependency {
                LibraryRange = new LibraryRange (
                    package.Identity.Id,
                    package.SupportedVersionRange,
                    LibraryDependencyTarget.Package),
            };
    }
}