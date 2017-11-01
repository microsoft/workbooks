//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Windows;

#if CLIENT_FLAVOR_INSPECTOR
[assembly: AssemblyTitle ("Xamarin Inspector")]
#else
[assembly: AssemblyTitle ("Xamarin Workbooks")]
#endif

[assembly: ThemeInfo (
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page, 
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
    //(used if a resource is not found in the page, 
    // app, or any theme specific resource dictionaries)
)]