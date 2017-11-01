// OptionsWindow.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Navigation;

using MahApps.Metro.Controls;

using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Client.Windows.Themes;
using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Windows.Views
{
	partial class OptionsWindow : MetroWindow, INotifyPropertyChanged
	{
		public enum Tab
		{
			General,
			Updater,
			Feedback
		}

		readonly IDisposable preferenceSubscription;

		public double WorkbookFontSize {
			get { return Prefs.UI.Font.GetSize (); }
			set {
				Prefs.UI.Font.Update (UIFontPreference.UpdateAction.Set, value);
				OnPropertyChanged ();
			}
		}

		public bool ShowLineNumbers {
			get { return Prefs.Editor.ShowLineNumbers.GetValue (); }
			set {
				Prefs.Editor.ShowLineNumbers.SetValue (value);
				OnPropertyChanged ();
			}
		}

		public bool ShowSubmissionExecutionTimings {
			get { return Prefs.Submissions.ShowExecutionTimings.GetValue (); }
			set {
				Prefs.Submissions.ShowExecutionTimings.SetValue (value);
				OnPropertyChanged ();
			}
		}

		public bool SaveHistory {
			get { return Prefs.Repl.SaveHistory.GetValue (); }
			set {
				Prefs.Repl.SaveHistory.SetValue (value);
				OnPropertyChanged ();
			}
		}

		public string [] AvailableThemes => ThemeHelper.Themes;

		public string CurrentTheme {
			get { return Prefs.UI.Theme.ThemeName.GetValue (); }
			set {
				Prefs.UI.Theme.ThemeName.SetValue (value);
				OnPropertyChanged ();
			}
		}

		public bool EnableTelemetry {
			get { return Prefs.Telemetry.Enabled.GetValue (); }
			set {
				Prefs.Telemetry.Enabled.SetValue (value);
				OnPropertyChanged ();
			}
		}

		public bool DisableTelemetry {
			get { return !Prefs.Telemetry.Enabled.GetValue (); }
			set {
				Prefs.Telemetry.Enabled.SetValue (!value);
				OnPropertyChanged ();
			}
		}

		public QueryFrequency UpdateQueryFrequency {
			get { return Prefs.Updater.QueryFrequency.GetValue (); }
			set {
				Prefs.Updater.QueryFrequency.SetValue (value);
				OnPropertyChanged ();
			}
		}

		public string UpdateChannel {
			get { return ClientApp.SharedInstance.Updater.UpdateChannel; }
			set {
				Prefs.Updater.Channel.SetValue (value);
				OnPropertyChanged ();
			}
		}

		public string [] AvailableUpdateChannels => ClientApp.SharedInstance.Updater.AvailableChannels;

		public string UpdateQueryLastRun {
			get {
				var time = Prefs.Updater.LastQuery.GetValue ();
				if (time == DateTime.MinValue)
					return Catalog.GetString ("Never");
				return time.ToLocalTime ().ToString ("f");
			}
		}

		public Uri MicrosoftPrivacyStatementUri => ClientInfo.MicrosoftPrivacyStatementUri;

		public bool IsInspectorClient => ClientInfo.Flavor == ClientFlavor.Inspector;

		public bool IsLocalDebugBuild => BuildInfo.IsLocalDebugBuild;

		public double MaxFontSize => Prefs.UI.Font.MaxFontSize;
		public double MinFontSize => Prefs.UI.Font.MinFontSize;

		public OptionsWindow ()
		{
			InitializeComponent ();

			DataContext = this;
			preferenceSubscription = PreferenceStore.Default.Subscribe (ObservePreferenceChange);

			telemetryNotice.Document = CommonMark.Formatters.FlowDocumentFormatter.Format (
				ClientInfo.TelemetryNotice.Parse (),
				ClientInfo.TelemetryNotice.UrlResolver,
				ClientInfo.TelemetryNotice.PlaceholderResolver);
		}

		public void SelectTab (Tab tab)
			=> tabControl.SelectedIndex = (int)tab;

		void ObservePreferenceChange (PreferenceChange change)
		{
			if (change.Key == Prefs.UI.Font.Key)
				OnPropertyChanged (nameof (WorkbookFontSize));
			else if (change.Key == Prefs.Editor.ShowLineNumbers.Key)
				OnPropertyChanged (nameof (ShowLineNumbers));
			else if (change.Key == Prefs.Submissions.ShowExecutionTimings.Key)
				OnPropertyChanged (nameof (ShowSubmissionExecutionTimings));
			else if (change.Key == Prefs.Repl.SaveHistory.Key)
				OnPropertyChanged (nameof (SaveHistory));
			else if (change.Key == Prefs.Telemetry.Enabled.Key)
				OnPropertyChanged (nameof (EnableTelemetry));
			else if (change.Key == Prefs.Updater.QueryFrequency.Key)
				OnPropertyChanged (nameof (UpdateQueryFrequency));
			else if (change.Key == Prefs.Updater.LastQuery.Key)
				OnPropertyChanged (nameof (UpdateQueryLastRun));
			else if (change.Key == Prefs.Updater.Channel.Key)
				OnPropertyChanged (nameof (UpdateChannel));
			else if (change.Key == Prefs.UI.Theme.ThemeName.Key)
				OnPropertyChanged (nameof (CurrentTheme));
		}

		void OnResetAllPreferences (object sender, RoutedEventArgs e)
			=> PreferenceStore.Default.RemoveAll ();

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged ([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

		protected override void OnClosed (EventArgs e)
		{
			base.OnClosed (e);

			preferenceSubscription.Dispose ();

			App.CheckNeedsExit ();
		}

		void Hyperlink_RequestNavigate (object sender, RequestNavigateEventArgs e)
			=> System.Diagnostics.Process.Start (e.Uri.ToString ());
	}
}
