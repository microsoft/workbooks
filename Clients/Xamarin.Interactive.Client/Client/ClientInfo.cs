//
// ClientInfo.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Reflection;

using CommonMark;
using CommonMark.Syntax;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client
{
	static class ClientInfo
	{
		const string TAG = nameof (ClientInfo);

		static ClientFlavor? flavor;

		public static ClientFlavor Flavor {
			get {
				if (flavor == null) {
					if (Assembly
						.GetEntryAssembly ()
						.GetName ()
						.Name
						.IndexOf ("workbook", StringComparison.OrdinalIgnoreCase) >= 0)
						flavor = ClientFlavor.Workbooks;
					else
						flavor = ClientFlavor.Inspector;
				}

				return flavor.Value;
			}
		}

		public static readonly string ShortProductName
			= Flavor == ClientFlavor.Inspector ? "Inspector" : "Workbooks";

		public static readonly string FullProductName = $"Xamarin {ShortProductName}";

		public static readonly string AboutBoxVersionString = Catalog.Format (Catalog.GetString (
			"Version {0} ({1}/{2})",
			comment: "{0} is a version number, {1} is a git branch, {2} is a git revision"),
			BuildInfo.Version.ToString (Versioning.ReleaseVersionFormat.FriendlyShort),
			BuildInfo.Branch,
			BuildInfo.HashShort);

		public static readonly string AboutBoxVersionTooltip = Catalog.Format (Catalog.GetString (
			"{0} built on {1:g}",
			comment: "{0} is a version number, {2} is a localized date ('D' format)"),
			BuildInfo.Version.ToString (),
			BuildInfo.Date);

		public static class TelemetryNotice
		{
			public static readonly CommonMarkSettings CommonMarkSettings;

			static TelemetryNotice ()
			{
				CommonMarkSettings = CommonMarkSettings.Default.Clone ();
				CommonMarkSettings.AdditionalFeatures |=
					CommonMarkAdditionalFeatures.PlaceholderBracket;
				CommonMarkSettings.UriResolver = UrlResolver;
			}

			public static string UrlResolver (string url)
			{
				switch (url) {
				case "PrivacyStatement":
					return MicrosoftPrivacyStatementUri.ToString ();
				default:
					return url;
				}
			}

			public static string PlaceholderResolver (string url)
			{
				switch (url) {
				case "FullProductName":
					return FullProductName;
				case "ShortProductName":
					return ShortProductName;
				default:
					return url;
				}
			}

			public static Block Parse ()
			{
				var settings = CommonMarkSettings.Default.Clone ();
				settings.AdditionalFeatures = CommonMarkAdditionalFeatures.PlaceholderBracket;
				using (var stream = typeof (ClientInfo).Assembly
					.GetManifestResourceStream ("TelemetryNotice.md"))
					return CommonMarkConverter.Parse (
						new StreamReader (stream),
						settings);
			}
		}

		public static readonly Uri MicrosoftPrivacyStatementUri = new Uri (
			"https://go.microsoft.com/fwlink/?LinkID=824704");

		public static readonly Uri HelpUri = new Uri (Flavor == ClientFlavor.Inspector
			? "https://developer.xamarin.com/guides/cross-platform/inspector/"
			: "https://developer.xamarin.com/guides/cross-platform/workbooks/");

		public static readonly Uri ForumsUri
			= new Uri ("https://forums.xamarin.com/categories/inspector");

		public static readonly Uri DownloadWorkbooksUri
			= new Uri ("https://developer.xamarin.com/workbooks");

		public static readonly string DownloadWorkbooksMenuLabel
			= Catalog.GetString ("Download More Workbooks…");
	}
}