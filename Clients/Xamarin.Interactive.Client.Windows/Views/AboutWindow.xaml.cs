// AboutWindow.xaml.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

using MahApps.Metro.Controls;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Windows.Views
{
	partial class AboutWindow : MetroWindow
	{
		public string ProductName { get; }

		public string Version { get; } = ClientInfo.AboutBoxVersionString;

		public string VersionTooltip { get; } = ClientInfo.AboutBoxVersionTooltip;

		public string Copyright { get; } = BuildInfo.Copyright;

		public Uri MicrosoftPrivacyStatementUri { get; } = ClientInfo.MicrosoftPrivacyStatementUri;

		public AboutWindow ()
		{
			ProductName = ClientInfo.ShortProductName;

			InitializeComponent ();
			DataContext = this;

			Title = $"About {ClientInfo.FullProductName}";
		}

		void OnHyperlinkClick (object sender, RoutedEventArgs args)
		{
			var hyperlink = sender as Hyperlink;
			var uri = hyperlink?.NavigateUri;

			if (uri == null)
				return;

			var fullUri = uri.OriginalString;

			if (!uri.IsAbsoluteUri)
				fullUri = Path.Combine (
					Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location),
					fullUri);

			try {
				Process.Start (fullUri);
			} catch (Exception e) {
				Log.Error ("AboutWindow", e);
			}
		}
	}
}
