//
// PackageIdComparer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using NuGet.Packaging.Core;

namespace Xamarin.Interactive.NuGet
{
	sealed class PackageIdComparer : IEqualityComparer<InteractivePackage>
	{
		public static bool Equals (string x, string y)
			=> string.Equals (x, y, StringComparison.OrdinalIgnoreCase);

		public static bool Equals (PackageIdentity x, PackageIdentity y)
			=> Equals (x?.Id, y?.Id);

		public static bool Equals (InteractivePackage x, InteractivePackage y)
			=> Equals (x?.Identity, y?.Identity);

		public static readonly IEqualityComparer<InteractivePackage> Default = new PackageIdComparer ();

		bool IEqualityComparer<InteractivePackage>.Equals (InteractivePackage x, InteractivePackage y)
			=> Equals (x, y);

		int IEqualityComparer<InteractivePackage>.GetHashCode (InteractivePackage obj)
			=> obj?.Identity.Id == null ? 0 : obj.Identity.Id.GetHashCode ();
	}
}