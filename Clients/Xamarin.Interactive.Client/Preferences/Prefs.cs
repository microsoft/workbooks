//
// Preferences.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Preferences
{
	static class Prefs
	{
		public static class Logging
		{
			public static readonly Preference<LogLevel> Level
				= new Preference<LogLevel> (
					"logging.level",
					#if DEBUG
					LogLevel.Debug
					#else
					LogLevel.Info
					#endif
				);
		}

		public static class UI
		{
			public static readonly UIFontPreference Font = new UIFontPreference ();

			public static class Theme
			{
				public static readonly Preference<string> ProseTypography
					= new Preference<string> ("ui.theme.proseTypography");

				public static readonly Preference<string> ThemeName
					= new Preference<string> ("ui.theme.themeName", "Light");

				public static readonly Preference<bool> UseHighContrast
					= new Preference<bool> ("ui.theme.useHighContrast", false);
			}
		}

		public static class Submissions
		{
			public static readonly Preference<bool> ReturnAtEndOfBufferExecutes
				= new Preference<bool> ("submissions.returnAtEndOfBufferExecutes", true);

			public static readonly Preference<bool> ShowExecutionTimings
				= new Preference<bool> ("submissions.showExecutionTimings", false);
		}

		public static class Repl
		{
			public static readonly Preference<bool> SaveHistory
				= new Preference<bool> ("repl.saveHistory", true);
		}

		public static class Editor
		{
			public static readonly Preference<bool> ShowLineNumbers
				= new Preference<bool> ("editor.showLineNumbers", true);
		}

		public static class Telemetry
		{
			public static readonly Preference<string> UserGuid
				= new Preference<string> ("telemetry.userGuid", Guid.NewGuid ().ToString ());

			public static readonly Preference<bool> Enabled
				= new Preference<bool> ("telemetry.enabled", true);
		}

		public static class Updater
		{
			public static readonly Preference<DateTime> LastQuery
				= new Preference<DateTime> ("updater.lastQuery");

			public static readonly Preference<Client.Updater.QueryFrequency> QueryFrequency
				= new Preference<Client.Updater.QueryFrequency> (
					"updater.queryFrequency",
					Client.Updater.QueryFrequency.Startup);

			public static readonly Preference<string> Channel
				= new Preference<string> ("updater.channel");
		}

		public static class Developer
		{
			public enum WebInspectorPane
			{
				Hidden,
				ElementTree,
				Console
			}

			public static class WebInspectorPaneNames
			{
				public static readonly string [] All = {
					Catalog.GetString ("Hidden"),
					Catalog.GetString ("Element Tree"),
					Catalog.GetString ("JavaScript Console")
				};

				public static string ToLocalizedName (WebInspectorPane frequency)
					=> All [(int)frequency];
			}

			public static readonly Preference<WebInspectorPane> StartupWebInspectorPane
				= new Preference<WebInspectorPane> (
					"developer.startupWebInspectorPane",
					WebInspectorPane.Hidden);

			public static readonly Preference<bool> MonitorCssChanges
				= new Preference<bool> (
					"developer.monitorCssChanges",
					false);
		}
	}
}