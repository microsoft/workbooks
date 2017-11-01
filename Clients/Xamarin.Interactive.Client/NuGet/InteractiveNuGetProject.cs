//
// InteractiveNuGetProject.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.NuGet
{
	/// <summary>
	/// A NuGet Project that extracts references MSBuild-style in a way that an InteractivePackageManager can
	/// consume. Doesn't actually install anything anywhere. Packages end up being referenced from the user's
	/// global package cache (typically ~/.nuget/packages).
	/// </summary>
	sealed class InteractiveNuGetProject : NuGetProject
	{
		readonly string packagesDirectory;

		public NuGetFramework TargetFramework { get; }

		public InteractiveInstallationContext InstallationContext { get; private set; }

		public InteractiveNuGetProject (NuGetFramework framework, ISettings settings)
		{
			if (framework == null)
				throw new ArgumentNullException (nameof (framework));
			if (settings == null)
				throw new ArgumentNullException (nameof (settings));

			packagesDirectory = SettingsUtility.GetGlobalPackagesFolder (settings);

			TargetFramework = framework;
			InternalMetadata [NuGetProjectMetadataKeys.TargetFramework] = framework;
			InternalMetadata [NuGetProjectMetadataKeys.Name] = nameof (InteractiveNuGetProject);
		}

		/// <summary>
		/// Should be called by the InteractivePackageManager managing this project before install/restore.
		/// </summary>
		public void ResetInstallationContext (InteractiveInstallationContext context = null)
			=> InstallationContext = context ?? new InteractiveInstallationContext ();

		public string Name => (string)InternalMetadata [NuGetProjectMetadataKeys.Name];

		public override Task<bool> InstallPackageAsync (
			PackageIdentity packageIdentity,
			DownloadResourceResult downloadResourceResult,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			if (InstallationContext == null)
				throw new NullReferenceException (
					"InstallationContext must be set before package install");
			if (packageIdentity == null)
				throw new ArgumentNullException (nameof (packageIdentity));
			if (downloadResourceResult == null)
				throw new ArgumentNullException (nameof (downloadResourceResult));
			if (nuGetProjectContext == null)
				throw new ArgumentNullException (nameof (nuGetProjectContext));

			var packageReader = downloadResourceResult.PackageReader ?? new PackageArchiveReader (
				downloadResourceResult.PackageStream,
				leaveStreamOpen: true);
			var packageAssemblyReferences = GetRelativePackageAssemblyReferences (packageReader);

			InstallationContext.AddInstalledPackage (new InteractivePackage (
				packageIdentity,
				isExplicit: false, // This gets determined by InteractivePackageManager
				assemblyReferences: RelativeToAbsolute (packageAssemblyReferences, packageIdentity)));

			return Task.FromResult (true);
		}

		public override Task<bool> UninstallPackageAsync (
			PackageIdentity packageIdentity,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			throw new NotImplementedException ();
		}

		public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationToken token)
			=> Task.FromResult (Enumerable.Empty<PackageReference> ());

		IReadOnlyList<string> GetRelativePackageAssemblyReferences (PackageReaderBase packageReader)
			=> Normalize (GetMostCompatibleGroup (TargetFramework, packageReader.GetReferenceItems ()))
				?.Items
				?.ToList ();

		public string GetInstalledPath (PackageIdentity packageIdentity)
		{
			if (packageIdentity == null)
				throw new ArgumentNullException (nameof (packageIdentity));
			if (!packageIdentity.HasVersion)
				throw new ArgumentException ("PackageIdentity.Version must be set");

			return FilePath.Build (
				packagesDirectory,
				packageIdentity.Id.ToLower (),
				packageIdentity.Version.ToNormalizedString ());
		}

		/// <summary>
		/// Convert relative reference paths to absolute paths. Requires the package
		/// to already be installed/restored.
		/// </summary>
		ImmutableList<FilePath> RelativeToAbsolute (
			IReadOnlyList<string> relativeReferences,
			PackageIdentity packageIdentity)
		{
			if (relativeReferences == null)
				return ImmutableList<FilePath>.Empty;

			var packageInstallPath = GetInstalledPath (packageIdentity);
			return relativeReferences
				.Select (r => FilePath.Build (packageInstallPath, r))
				.ToImmutableList ();
		}

		/// <summary>
		/// Get absolute paths to the package's assembly references. Requires the package
		/// to already be installed/restored.
		/// </summary>
		public ImmutableList<FilePath> GetPackageAssemblyReferences (
			PackageReaderBase packageReader,
			PackageIdentity packageIdentity)
			=> RelativeToAbsolute (
				GetRelativePackageAssemblyReferences (packageReader),
				packageIdentity);

		#region  Copied from internal MSBuildNuGetProjectSystemUtility stuff
		internal static FrameworkSpecificGroup GetMostCompatibleGroup (
			NuGetFramework projectTargetFramework,
			IEnumerable<FrameworkSpecificGroup> itemGroups)
		{
			var reducer = new FrameworkReducer ();
			var mostCompatibleFramework = reducer.GetNearest (
				projectTargetFramework,
				itemGroups.Select (i => i.TargetFramework));
			if (mostCompatibleFramework != null) {
				var mostCompatibleGroup = itemGroups.FirstOrDefault (
					i => i.TargetFramework.Equals (mostCompatibleFramework));

				if (IsValid (mostCompatibleGroup)) {
					return mostCompatibleGroup;
				}
			}

			return null;
		}

		/// <summary>
		/// Filter out invalid package items and replace the directory separator with the correct slash for the 
		/// current OS.
		/// </summary>
		/// <remarks>If the group is null or contains only only _._ this method will return the same group.
		/// </remarks>
		internal static FrameworkSpecificGroup Normalize (FrameworkSpecificGroup group)
		{
			// Default to returning the same group
			var result = group;

			// If the group is null or it does not contain any items besides _._ then this is a no-op.
			// If it does have items create a new normalized group to replace it with.
			if (group?.Items.Any () == true) {
				// Filter out invalid files
				var normalizedItems =
					GetValidPackageItems (group.Items)
						.Select (item => PathUtility.ReplaceAltDirSeparatorWithDirSeparator (
							item));

				// Create a new group
				result = new FrameworkSpecificGroup (
					targetFramework: group.TargetFramework,
					items: normalizedItems);
			}

			return result;
		}

		internal static bool IsValid (FrameworkSpecificGroup frameworkSpecificGroup)
		{
			if (frameworkSpecificGroup != null) {
				return (frameworkSpecificGroup.HasEmptyFolder ||
					frameworkSpecificGroup.Items.Any () ||
					!frameworkSpecificGroup.TargetFramework.Equals (NuGetFramework.AnyFramework));
			}

			return false;
		}

		internal static IEnumerable<string> GetValidPackageItems (IEnumerable<string> items)
		{
			if (items == null
				|| !items.Any ()) {
				return Enumerable.Empty<string> ();
			}

			// Assume nupkg and nuspec as the save mode for identifying valid package files
			return items.Where (i => PackageHelper.IsPackageFile (i, PackageSaveMode.Defaultv3));
		}
		#endregion
	}
}
