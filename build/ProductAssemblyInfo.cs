//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyCopyright (
    "Copyright 2016-2018 Microsoft. All rights reserved.\n" +
    "Copyright 2014-2016 Xamarin Inc. All rights reserved.")]
[assembly: AssemblyVersion ("1.0.0.0")]

// Spells 0.A.F.V, a magic version that UpdateBuildInfo will
// replace with a real file version (see BuildInfoTask).
[assembly: AssemblyFileVersion ("0.65.70.86")]

#if !PRODUCT_ASSEMBLY_INFO_OMIT_IVT

// agents
[assembly: InternalsVisibleTo ("Xamarin.Interactive.DotNetCore")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Mac.Desktop")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Mac.Mobile")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Wpf")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Console")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms.Android")]

// code analysis
[assembly: InternalsVisibleTo ("Xamarin.Interactive.CodeAnalysis")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.CodeAnalysis.Tests")]

// clients
[assembly: InternalsVisibleTo ("workbook")]
[assembly: InternalsVisibleTo ("workbooks-server")]
[assembly: InternalsVisibleTo ("xic")]
[assembly: InternalsVisibleTo ("Xamarin Workbooks")]
[assembly: InternalsVisibleTo ("Xamarin Inspector")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Client")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Client.Console")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Client.Desktop")]

// tests
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests.Core")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests.Mac")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests.Windows")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests.Android")]

// workbook apps
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.DotNetCore")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Forms.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Forms.iOS")]
[assembly: InternalsVisibleTo ("Workbook Mac App (Desktop Profile)")]
[assembly: InternalsVisibleTo ("Workbook Mac App (Mobile Profile)")]
[assembly: InternalsVisibleTo ("Xamarin Workbooks Agent (WPF)")]

// workbook app client integrations
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Client.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Client.Android")]

// IDE extensions
[assembly: InternalsVisibleTo ("Xamarin.Interactive.XS")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.VS")]

// misc
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Telemetry.Server")]

#endif