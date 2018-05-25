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

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.NuGet
{
    sealed class InteractivePackageManager : INotifyPropertyChanged
    {
        const string TAG = nameof (InteractivePackageManager);

        internal const string IntegrationPackageId = "Xamarin.Workbooks.Integration";

        internal static readonly InteractivePackageDescription FixedXamarinFormsPackageDescription
            = new InteractivePackageDescription ("Xamarin.Forms", "3.0.0.482510");

        internal static readonly PackageIdentity FixedXamarinFormsPackageIdentity
            = FixedXamarinFormsPackageDescription.ToPackageIdentity ();

        readonly NuGetPackageManager packageManager;
        readonly InteractivePackageProjectContext projectContext;
        readonly InteractiveNuGetProject project;
        readonly ISettings settings;
        readonly FilePath packageConfigDirectory;

        ImmutableHashSet<InteractivePackage> installedPackages = ImmutableHashSet<InteractivePackage>
            .Empty
            .WithComparer (PackageIdComparer.Default);

        ImmutableArray<InteractivePackage> sortedInstalledPackages = ImmutableArray<InteractivePackage>.Empty;
        public ImmutableArray<InteractivePackage> InstalledPackages => sortedInstalledPackages;

        public string RuntimeIdentifier { get; }
        public NuGetFramework TargetFramework { get; }
        public ImmutableArray<SourceRepository> SourceRepositories { get; }
        public ILogger Logger { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <param name="packageConfigDirectory">A directory where the package manager can store temporary
        /// files, and where nuget.config settings files could be placed specific to Workbooks.</param>
        public InteractivePackageManager (
            string runtimeIdentifier,
            FrameworkName targetFramework,
            FilePath packageConfigDirectory)
        {
            if (targetFramework == null)
                throw new ArgumentNullException (nameof (targetFramework));

            if (packageConfigDirectory.IsNull)
                throw new ArgumentNullException (nameof (packageConfigDirectory));

            RuntimeIdentifier = runtimeIdentifier;

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

        public FilePath GetPackageInstallPath (InteractivePackage package)
            => project.GetInstalledPath (package.Identity);

        public Task<IReadOnlyList<InteractivePackage>> RemoveAsync (
            InteractivePackage package,
            CancellationToken cancellationToken = default)
            => RemoveAsync (
                installedPackages.Remove (package),
                cancellationToken);

        public Task<IReadOnlyList<InteractivePackage>> RemoveAsync (
            IEnumerable<InteractivePackage> packages,
            CancellationToken cancellationToken = default)
            => RestoreAsync (
                installedPackages.Except (packages),
                cancellationToken);

        public Task<IReadOnlyList<InteractivePackage>> InstallAsync (
            InteractivePackage package,
            CancellationToken cancellationToken = default)
            => InstallAsync (
                installedPackages.Add (package),
                cancellationToken);

        public Task<IReadOnlyList<InteractivePackage>> InstallAsync (
            IEnumerable<InteractivePackage> packages,
            CancellationToken cancellationToken = default)
            => RestoreAsync (
                installedPackages.Union (packages),
                cancellationToken);

        public Task<IReadOnlyList<InteractivePackage>> RestoreAsync (
            IEnumerable<InteractivePackage> packages,
            CancellationToken cancellationToken = default)
            => RestoreAsync (
                packages,
                false,
                cancellationToken);

        public async Task<IReadOnlyList<InteractivePackage>> RestoreAsync (
            IEnumerable<InteractivePackage> packages,
            bool forceRestore,
            CancellationToken cancellationToken = default)
        {
            if (!forceRestore && installedPackages
                .WithComparer (PackageIdVersionComparer.Default)
                .SetEquals (packages))
                return sortedInstalledPackages;

            using (var cacheContext = new SourceCacheContext ()) {
                if (!(await RestoreAsync (
                    FixupPackages (packages, log: true),
                    cacheContext,
                    cancellationToken).ConfigureAwait (false)))
                    return sortedInstalledPackages;
            }

            installedPackages = project.InstallationContext.InstalledPackages;

            sortedInstalledPackages = installedPackages
                .OrderBy (p => p.Identity.Id)
                .ToImmutableArray ();

            // Initiate another restore against the packages we just restored. This
            // allows TryFixupPackage to run against any newly restored packages,
            // and if any fixups were applied (due to transitive dependencies not
            // able to be fixed up on the previous pass), restore again with the
            // new fixups applied. This will happen recursively until no packages
            // need fixing.
           await RestoreAsync (
                FixupPackages (installedPackages, log: false),
                cancellationToken).ConfigureAwait (false);

            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (nameof (InstalledPackages)));

            return sortedInstalledPackages;
        }

        async Task<bool> RestoreAsync (
            IEnumerable<InteractivePackage> packages,
            SourceCacheContext cacheContext,
            CancellationToken cancellationToken)
        {
            packages = packages.ToList ();

            // NOTE: This path is typically empty. It could in theory contain nuget.config
            // settings files, but really we just use it to satisfy nuget API that requires
            // paths, even when they are never used.
            var rootPath = packageConfigDirectory;

            // Set up a project spec similar to what you would see in a project.json.
            // This is sufficient for the dependency graph work done within RestoreCommand.
            var restoreContext = new RestoreArgs {
                CacheContext = cacheContext,
                Log = Logger,
            };

            var restoreResult = await new RestoreCommand (
                new RestoreRequest (
                    new PackageSpec (new [] {
                        new TargetFrameworkInformation {
                            FrameworkName = TargetFramework,
                            Dependencies = packages
                                .Select (ToLibraryDependency)
                                .ToList ()
                        }
                    }) {
                        Name = project.Name,
                        FilePath = rootPath,
                    },
                    new RestoreCommandProvidersCache ()
                        .GetOrCreate (
                            restoreContext.GetEffectiveGlobalPackagesFolder (rootPath, settings),
                            restoreContext.GetEffectiveFallbackPackageFolders (settings),
                            SourceRepositories,
                            cacheContext,
                            Logger),
                    cacheContext,
                    Logger)).ExecuteAsync (cancellationToken).ConfigureAwait (false);

            if (!restoreResult.Success)
                return false;

            project.ResetInstallationContext ();

            // As with installation, restore simply ensures that packages are present in the user's
            // global package cache. We reference them out of there just like .NET core projects do.
            //
            // All resolved packages, including the explicit inputs and their dependencies, are
            // available as LockFileLibrary instances.
            foreach (var library in restoreResult.LockFile.Libraries)
                project.InstallationContext.AddInstalledPackage (
                    GetInteractivePackageFromLibrary (library, project, packages));

            return true;
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

        static IEnumerable<InteractivePackage> FixupPackages (IEnumerable<InteractivePackage> packages, bool log)
            => packages
                .Select (package => {
                    TryFixupPackage (ref package, log);
                    return package;
                })
                .Where (package => package != null);

        static bool TryFixupPackage (ref InteractivePackage package, bool log)
        {
            if (PackageIdComparer.Equals (package.Identity.Id, IntegrationPackageId)) {
                if (log)
                    Log.Warning (TAG, $"Refusing to add integration NuGet package {IntegrationPackageId}.");
                package = null;
                return true;
            }

            // Pin Xamarin.Forms to what we have in the Xamarin.Forms workbook apps
            if (PackageIdComparer.Equals (package.Identity.Id, FixedXamarinFormsPackageIdentity.Id) &&
                package.Identity.Version != FixedXamarinFormsPackageIdentity.Version) {
                if (log)
                    Log.Warning (
                        TAG,
                        $"Replacing requested Xamarin.Forms version {package.Identity.Version} with " +
                        $"required version {FixedXamarinFormsPackageIdentity.Version}.");
                package = package.WithVersion (
                    FixedXamarinFormsPackageIdentity.Version,
                    overwriteRange: true);
                return true;
            }

            return false;
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