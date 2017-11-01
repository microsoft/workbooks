//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;

using Xamarin.Interactive.Collections;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Workbook.Structure
{
    sealed class NuGetPackagesNode : TreeNode
    {
        public static readonly RoutedUICommand AddPackage = new RoutedUICommand (
            Catalog.GetString ("Add Package…"),
            "addPackage:",
            typeof (NuGetPackagesNode));

        public new ObservableCollection<NuGetPackageNode> Children
            => (ObservableCollection<NuGetPackageNode>)base.Children;

        public NuGetPackagesNode ()
        {
            IconName = "folder-component";
            Name = Catalog.GetString ("NuGet Packages");
            base.Children = new ObservableCollection<NuGetPackageNode> ();

            Commands = new [] {
                AddPackage
            };

            DefaultCommand = AddPackage;
        }

        public void UpdateChildren (IEnumerable<InteractivePackage> packages)
            => Children.UpdateTo (packages.Select (package => Children.FirstOrDefault (
                n => n.RepresentedObject == package) ?? new NuGetPackageNode (package)).ToList ());
    }
}