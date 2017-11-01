//
// NuGetPackageNode.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Windows.Input;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Workbook.Structure
{
	sealed class NuGetPackageNode : TreeNode
	{
		public static RoutedUICommand RemovePackage = new RoutedUICommand (
			Catalog.GetString ("Remove Package"),
			"Remove",
			typeof (NuGetPackageNode));

		public NuGetPackageNode (InteractivePackage package)
		{
			IconName = "nuget";
			RepresentedObject = package;
			Name = package.Identity.Id;
			// Version is set if the package has been restored, otherwise use SupportedVersionRange to show
			// what's in the manifest.
			ToolTip = $"{Name} {package.Identity.Version?.ToString () ?? package.SupportedVersionRange?.ToString ()}";

			Commands = new [] {
				RemovePackage
			};
		}
	}
}
