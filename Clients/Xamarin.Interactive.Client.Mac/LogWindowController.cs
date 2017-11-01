//
// LogWindowController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Foundation;
using AppKit;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Mac
{
	sealed partial class LogWindowController : NSWindowController
	{
		static NSColor MakeColor (byte r, byte g, byte b)
		{
			return NSColor.FromCalibratedRgba (r / 255f, g / 255f, b / 255f, 1);
		}

		static readonly NSColor green = MakeColor (94, 124, 3);
		static readonly NSColor blue = MakeColor (50, 109, 192);
		static readonly NSColor purple = MakeColor (116, 55, 161);
		static readonly NSColor red = MakeColor (185, 20, 31);
		static readonly NSColor orange = MakeColor (240, 141, 25);
		static readonly NSColor gray = MakeColor (100, 100, 100);
		static readonly NSColor darkGray = MakeColor (40, 40, 40);

		public LogWindowController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public LogWindowController (NSCoder coder) : base (coder)
		{
		}

		public LogWindowController () : base ("LogWindow")
		{
		}

		readonly List<string> logOwnersBeforeWindowDidLoad = new List<string> ();

		public new LogWindow Window {
			get { return (LogWindow)base.Window; }
		}

		public void AppendLogEntry (LogEntry entry)
		{
			BeginInvokeOnMainThread (() => {
				AppendLogOwner (entry);

				if (logTableView != null) {
					var logDataSource = (LogTableViewDataSource)logTableView.DataSource;
					logDataSource.Refresh ();

					logTableView.ReloadData ();
					var rows = logTableView.DataSource.GetRowCount (logTableView) - 1;
					logTableView.ScrollRowToVisible (rows);
				}
			});
		}

		string GetNameForOwnerId (string ownerId)
		{
			return ownerId ?? "Client";
		}

		void AppendLogOwner (LogEntry entry)
		{
			if (logOwnerPopUpButton == null) {
				if (!logOwnersBeforeWindowDidLoad.Contains (entry.OwnerId))
					logOwnersBeforeWindowDidLoad.Add (entry.OwnerId);
				return;
			}

			var items = logOwnerPopUpButton.Items ();
			if (items == null || items.Length == 0) {
				logOwnerPopUpButton.AddItem ("All Logs");
				logOwnerPopUpButton.Menu.AddItem (NSMenuItem.SeparatorItem);
				foreach (var ownerId in logOwnersBeforeWindowDidLoad)
					logOwnerPopUpButton.AddItem (GetNameForOwnerId (ownerId));
				logOwnersBeforeWindowDidLoad.Clear ();
			}

			var ownerName = GetNameForOwnerId (entry.OwnerId);

			foreach (var item in items) {
				if (item.Title == ownerName)
					return;
			}

			logOwnerPopUpButton.AddItem (ownerName);
		}

		public override void WindowDidLoad ()
		{
			logTableView.Delegate = new LogTableViewDelegate ();
			logTableView.DataSource = new LogTableViewDataSource ();

			logOwnerPopUpButton.Activated += UpdateFilter;
			logSearchField.Changed += UpdateFilter;
		}

		void UpdateFilter (object sender, EventArgs e)
		{
			var logDataSource = (LogTableViewDataSource)logTableView.DataSource;

			Func<LogEntry, bool> ownerFilter = null;
			Func<LogEntry, bool> searchFilter = null;

			if (logOwnerPopUpButton.IndexOfSelectedItem >= 2) {
				var ownerName = logOwnerPopUpButton.TitleOfSelectedItem;
				ownerFilter = entry => GetNameForOwnerId (entry.OwnerId) == ownerName;
			}

			var searchString = logSearchField.StringValue;
			if (!String.IsNullOrWhiteSpace (searchString))
				searchFilter = entry => entry.ToString ().IndexOf (searchString,
					StringComparison.InvariantCultureIgnoreCase) >= 0;

			if (ownerFilter != null && searchFilter != null)
				logDataSource.Filter = entry => ownerFilter (entry) && searchFilter (entry);
			else if (ownerFilter == null)
				logDataSource.Filter = searchFilter;
			else
				logDataSource.Filter = ownerFilter;

			logTableView.ReloadData ();
		}

		class LogTableViewDataSource : NSTableViewDataSource
		{
			IReadOnlyList<LogEntry> filteredList;

			Func<LogEntry, bool> filter;
			public Func<LogEntry, bool> Filter {
				get { return filter ?? (e => true); }
				set {
					filter = value;
					Refresh ();
				}
			}

			public LogTableViewDataSource ()
			{
				Refresh ();
			}

			public void Refresh ()
			{
				// Cache the filtered list because when debugging, the query is very slow to run
				// repeatedly.
				filteredList = Log.GetEntries ().Where (Filter).ToList ();
			}

			public override nint GetRowCount (NSTableView tableView)
			{
				return filteredList.Count ();
			}

			public LogEntry GetEntry (int row)
			{
				return filteredList.Skip (row).First ();
			}
		}

		class LogTableViewDelegate : NSTableViewDelegate
		{
			public override NSView GetViewForItem (NSTableView tableView,
				NSTableColumn tableColumn, nint row)
			{
				var view = tableView.MakeView (tableColumn.Identifier, this) as LogTableCellView;
				if (view == null)
					return null; 

				view.TextColor = NSColor.Text;

				var entry = ((LogTableViewDataSource)tableView.DataSource).GetEntry ((int)row);
				var textField = view.TextField;

				textField.Font = NSFont.UserFixedPitchFontOfSize (12);

				switch (tableColumn.Identifier) {
				case "log-owner":
					textField.StringValue = entry.OwnerId ?? String.Empty;
					view.TextColor = gray;
					break;
				case "log-level":
					textField.StringValue = entry.Level.ToString ();

					switch (entry.Level) {
					case LogLevel.Critical:
						view.TextColor = red;
						break;
					case LogLevel.Error:
						view.TextColor = red;
						break;
					case LogLevel.Warning:
						view.TextColor = orange;
						break;
					case LogLevel.Info:
						view.TextColor = green;
						break;
					case LogLevel.Debug:
						view.TextColor = blue;
						break;
					case LogLevel.Verbose:
						view.TextColor = darkGray;
						break;
					}
					break;
				case "log-time":
					textField.StringValue = entry.RelativeTime.TotalSeconds.ToString ();
					view.TextColor = gray;
					break;
				case "log-tag":
					textField.StringValue = entry.Tag ?? String.Empty;
					view.TextColor = purple;
					break;
				case "log-message":
					textField.StringValue = entry.Message ?? String.Empty;
					view.TextColor = darkGray;
					break;
				}

				return view;
			}
		}

		[Register ("LogTableCellView")]
		class LogTableCellView : NSTableCellView
		{
			public NSColor TextColor { get; set; }

			[Export ("initWithCoder:")]
			public LogTableCellView (NSCoder coder) : base (coder)
			{
			}

			public override NSBackgroundStyle BackgroundStyle {
				get { return base.BackgroundStyle; }
				set {
					base.BackgroundStyle = value;
					TextField.TextColor = ((NSTableRowView)Superview).Selected
						? NSColor.SelectedText
						: (TextColor ?? NSColor.Text);
				}
			}
		}

		public override bool RespondsToSelector (ObjCRuntime.Selector sel)
		{
			if (sel.Name == "copy:")
				return logTableView.SelectedRowCount > 0;

			return base.RespondsToSelector (sel);
		}

		[Export ("copy:")]
		void Copy (NSObject sender)
		{
			var builder = new StringBuilder ();
			foreach (var row in logTableView.SelectedRows) {
				var entry = ((LogTableViewDataSource)logTableView.DataSource).GetEntry ((int)row);
				builder.AppendLine (entry.ToString ());
			}
			builder.Length--; // remove trailing newline

			string[] types = { "NSStringPboardType" };
			NSPasteboard.GeneralPasteboard.DeclareTypes (types, null);
			NSPasteboard.GeneralPasteboard.SetStringForType (builder.ToString (), types [0]);
		}
	}
}