// IPackageManagerView.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.

namespace Xamarin.Interactive.NuGet
{
	interface IPackageManagerView
	{
		void ClearPackages ();
		void AddPackageResult (PackageViewModel package);
	}
}
