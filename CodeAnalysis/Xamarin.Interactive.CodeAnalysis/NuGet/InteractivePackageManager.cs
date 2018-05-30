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
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.NuGet
{
    sealed class InteractivePackageManager
    {
        const string TAG = nameof (InteractivePackageManager);

        internal const string IntegrationPackageId = "Xamarin.Workbooks.Integration";

        internal static readonly InteractivePackageDescription FixedXamarinFormsPackageDescription
            = new InteractivePackageDescription ("Xamarin.Forms", "3.0.0.482510");

        internal static readonly PackageIdentity FixedXamarinFormsPackageIdentity
            = new PackageIdentity (
                FixedXamarinFormsPackageDescription.PackageId,
                NuGetVersion.Parse (FixedXamarinFormsPackageDescription.VersionRange));

        readonly NuGetPackageManager packageManager;
        readonly InteractivePackageProjectContext projectContext;
        readonly InteractiveNuGetProject project;
        readonly ISettings settings;
        readonly FilePath packageConfigDirectory;

        public string RuntimeIdentifier { get; }
        public NuGetFramework TargetFramework { get; }
        public ImmutableArray<SourceRepository> SourceRepositories { get; }
        public ILogger Logger { get; }

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

        public async Task<IReadOnlyList<InteractivePackage>> RestoreAsync (
            PackageReferenceList packageReferences,
            CancellationToken cancellationToken = default)
        {
            (bool restoreNeeded, IReadOnlyList<InteractivePackage> packages) restoreOperation = (
                true,
                packageReferences
                    .Select (InteractivePackage.FromPackageReference)
                    .ToList ());

            int i = 0;
            while (restoreOperation.restoreNeeded) {
                using (var cacheContext = new SourceCacheContext ())
                    restoreOperation = await RestoreAsync (
                        packageReferences,
                        restoreOperation.packages,
                        cacheContext,
                        cancellationToken).ConfigureAwait (false);

                if (++i >= 10) {
                    Log.Warning (TAG, "Restore has reached the maximum number of restore attempts; " +
                        "package fixups may not be applicable to this graph");
                    break;
                }
            }

            return restoreOperation.packages;
        }

        async Task<(bool fixesApplied, IReadOnlyList<InteractivePackage>)> RestoreAsync (
            PackageReferenceList packageReferences,
            IEnumerable<InteractivePackage> packages,
            SourceCacheContext cacheContext,
            CancellationToken cancellationToken)
        {
            var restoreTarget = await RestoreAsync (
                packages,
                cacheContext,
                cancellationToken).ConfigureAwait (false);

            if (restoreTarget == null)
                return (false, Array.Empty<InteractivePackage> ());

            var fixesApplied = false;
            var restoredPackages = ImmutableList.CreateBuilder<InteractivePackage> ();

            foreach (var library in restoreTarget.Libraries) {
                var packageIdentity = new PackageIdentity (library.Name, library.Version);
                var assemblyReferences = ImmutableList<FilePath>.Empty;

                if (TryFixupPackage (ref packageIdentity)) {
                    fixesApplied = true;
                    if (packageIdentity == null)
                        continue;
                } else {
                    var packageInstallPath = project.GetInstalledPath (packageIdentity);
                    var compileTimeAssemblies = library.CompileTimeAssemblies.ToImmutableHashSet ();
                    assemblyReferences = library
                        .CompileTimeAssemblies
                        .Select (assembly => packageInstallPath.Combine (assembly.Path))
                        .Where (path => path.Name != "_._")
                        .ToImmutableList ();
                }

                packageReferences.TryGetValue (
                    packageIdentity.Id,
                    out var userPackageReference);

                restoredPackages.Add (new InteractivePackage (
                    packageIdentity,
                    userPackageReference,
                    assemblyReferences));
            }

            return (fixesApplied, restoredPackages.ToImmutable ());
        }

        async Task<LockFileTarget> RestoreAsync (
            IEnumerable<InteractivePackage> packages,
            SourceCacheContext cacheContext,
            CancellationToken cancellationToken)
        {
            // NOTE: This path is typically empty. It could in theory contain nuget.config
            // settings files, but really we just use it to satisfy nuget API that requires
            // paths, even when they are never used.
            var rootPath = packageConfigDirectory;

            // Set up a project spec similar to what you would see in a project.json.
            // This is sufficient for the dependency graph work done within RestoreCommand.
            var restoreContext = new RestoreArgs {
                CacheContext = cacheContext,
                Log = Logger
            };

            var restoreRequest = new RestoreRequest (
                new PackageSpec (new [] {
                    new TargetFrameworkInformation {
                        FrameworkName = TargetFramework,
                        Dependencies = packages
                            .Select (package => package.ToLibraryDependency ())
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
                Logger);

            if (RuntimeIdentifier != null)
                restoreRequest.RequestedRuntimes.Add (RuntimeIdentifier);

            var restoreResult = await new RestoreCommand (restoreRequest)
                .ExecuteAsync (cancellationToken)
                .ConfigureAwait (false);

            if (!restoreResult.Success)
                return null;

            return restoreResult.LockFile.GetTarget (TargetFramework, RuntimeIdentifier)
                ?? restoreResult.LockFile.Targets.FirstOrDefault ();
        }

        static bool TryFixupPackage (ref PackageIdentity identity)
        {
            if (PackageIdComparer.Equals (identity.Id, IntegrationPackageId)) {
                Log.Warning (TAG, $"Refusing to add integration NuGet package {IntegrationPackageId}.");
                identity = null;
                return true;
            }

            // Pin Xamarin.Forms to what we have in the Xamarin.Forms workbook apps
            if (PackageIdComparer.Equals (identity.Id, FixedXamarinFormsPackageIdentity.Id) &&
                identity.Version != FixedXamarinFormsPackageIdentity.Version) {
                Log.Warning (
                    TAG,
                    $"Replacing requested Xamarin.Forms version {identity.Version} with " +
                    $"required version {FixedXamarinFormsPackageIdentity.Version}.");

                identity = new PackageIdentity (
                    identity.Id,
                    FixedXamarinFormsPackageIdentity.Version);
                return true;
            }

            return false;
        }
    }
}