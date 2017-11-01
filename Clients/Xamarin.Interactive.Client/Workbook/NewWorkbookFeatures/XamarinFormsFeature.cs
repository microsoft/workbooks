//
// XamarinFormsFeature.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Workbook.NewWorkbookFeatures
{
	sealed class XamarinFormsFeature : NewWorkbookFeature
	{
		public static readonly XamarinFormsFeature SharedInstance = new XamarinFormsFeature ();

		XamarinFormsFeature ()
		{
		}

		public override string Id { get; } = "xamarin.forms";

		public override string Label { get; }
			= Catalog.GetString ("Xamarin.Forms");

		public override string Description { get; }
			= Catalog.GetString ("Configure this workbook with support for Xamarin.Forms.");

		public override Task ConfigureClientSession (
			ClientSession clientSession,
			CancellationToken cancellationToken = default (CancellationToken))
			=> clientSession.InstallPackageAsync (
				new PackageViewModel (
					InteractivePackageManager.FixedXamarinFormsPackageIdentity));
	}
}