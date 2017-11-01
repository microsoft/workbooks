//
// InteractivePackage.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.NuGet
{
	sealed class InteractivePackage
	{
		/// <summary>
		/// The NuGet package identity contains the package ID and (if set) the NuGetVersion.
		/// Typically the NuGetVersion will not be set if this package came from the manifest
		/// and has not been updated via package restore.
		/// </summary>
		public PackageIdentity Identity { get; }

		/// <summary>
		/// The supported versions specified in the workbook manifest.
		/// https://docs.microsoft.com/en-us/nuget/create-packages/dependency-versions#version-ranges
		/// </summary>
		public VersionRange SupportedVersionRange { get; }

		public ImmutableList<FilePath> AssemblyReferences { get; }
		public bool IsExplicit { get; }

		/// <summary>
		/// Construct an InteractivePackage from the workbook manifest. Identity.Version will not be set.
		/// Restoring this package will return a new instance with the installed version set.
		/// </summary>
		/// <param name="id">The NuGet package ID.</param>
		/// <param name="supportedVersionRange">The supported versions specified in the manifest.</param>
		/// <param name="isExplicit">Indicates that this is a user-specified package, and should persist
		/// in the manifest. Defaults to true. False indicates that this is a dependency or some other
		/// hidden package for the workbook.</param>
		public InteractivePackage (string id, VersionRange supportedVersionRange, bool isExplicit = true)
			: this (new PackageIdentity (id, null), isExplicit, null, supportedVersionRange)
		{
			if (supportedVersionRange == null)
				throw new ArgumentNullException (nameof (supportedVersionRange));
		}

		public InteractivePackage (
			PackageIdentity packageIdentity,
			bool isExplicit = true,
			ImmutableList<FilePath> assemblyReferences = null,
			VersionRange supportedVersionRange = null)
		{
			if (packageIdentity == null)
				throw new ArgumentNullException (nameof (packageIdentity));

			Identity = packageIdentity;
			IsExplicit = isExplicit;
			AssemblyReferences = assemblyReferences ?? ImmutableList<FilePath>.Empty;

			if (supportedVersionRange != null || Identity.Version == null)
				SupportedVersionRange = supportedVersionRange;
			else
				// Use Parse to force VersionRange.OriginalString to be set, so we
				// don't end up writing "[9.0.1, )]" instead of "9.0.1" to manifest
				SupportedVersionRange = VersionRange.Parse (Identity.Version.ToNormalizedString ());
		}

		public InteractivePackage AddAssemblyReference (
			FilePath assemblyReferencePath)
		{
			if (assemblyReferencePath.IsNull)
				return this;

			if (AssemblyReferences.Contains (assemblyReferencePath))
				return this;

			return new InteractivePackage (
				Identity,
				IsExplicit,
				AssemblyReferences.Add (assemblyReferencePath),
				SupportedVersionRange);
		}

		public InteractivePackage WithIsExplicit (bool isExplicit)
		{
			if (isExplicit == IsExplicit)
				return this;

			return new InteractivePackage (
				Identity,
				isExplicit,
				AssemblyReferences,
				SupportedVersionRange);
		}

		public InteractivePackage WithSupportedVersionRange (VersionRange supportedVersionRange)
		{
			if (SupportedVersionRange == supportedVersionRange)
				return this;

			return new InteractivePackage (
				Identity,
				IsExplicit,
				AssemblyReferences,
				supportedVersionRange);
		}

		public InteractivePackage WithVersion (NuGetVersion version, bool overwriteRange = false)
		{
			if (Identity.Version == version)
				return this;

			return new InteractivePackage (
				new PackageIdentity (Identity.Id, version),
				IsExplicit,
				AssemblyReferences,
				overwriteRange ? null : SupportedVersionRange);
		}
	}
}