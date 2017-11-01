//
// PackageIdentityExtensions.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace NuGet.Packaging.Core
{
	static class PackageIdentityExtensions
	{
		public static string GetFullName (this PackageIdentity package)
			=> $"{package.Id} {package.Version?.ToNormalizedString ()}";
	}
}
