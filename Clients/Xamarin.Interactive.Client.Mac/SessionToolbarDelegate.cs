//
// SessionToolbarDelegate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using AppKit;
using CoreGraphics;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Mac
{
	sealed class SessionToolbarDelegate : NSToolbarDelegate
	{
		static class Identifiers
		{
			public const string RunAll = "xi-toolbar-run-all";
			public const string Refresh = "xi-toolbar-refresh";
			public const string Inspect = "xi-toolbar-inspect";
			public const string TargetSelector = "xi-toolbar-target-selector";
			public const string CenteringSpacer = "xi-toolbar-centering-spacer";
			public const string Status = "xi-status";
			public const string FlexSpacer = "NSToolbarFlexibleSpaceItem";
			public const string TabView = "xi-toolbar-tab-view";

			public static readonly string [] WorkbooksDefault = {
				RunAll,
				TargetSelector,
				CenteringSpacer,
				Status,
				FlexSpacer,
				TabView
			};

			public static readonly string [] InspectorDefault = {
				CenteringSpacer,
				Status,
				FlexSpacer,
				TabView
			};
		}

		readonly NSToolbar toolbar;
		readonly string [] allowedItemIdentifiers;

		readonly NSToolbarItem runAllItem;
		readonly NSToolbarItem refreshItem;
		readonly NSToolbarItem inspectItem;
		readonly NSToolbarItem targetSelectorItem;
		readonly CenteringToolbarItem centeringItem;
		readonly StatusToolbarItem statusItem;
		readonly NSToolbarItem tabViewItem;

		public SessionToolbarDelegate (
			ClientSession clientSession,
			MacClientSessionViewControllers viewControllers,
			NSToolbar toolbar)
		{
			if (clientSession == null)
				throw new ArgumentNullException (nameof (clientSession));

			if (viewControllers == null)
				throw new ArgumentNullException (nameof (viewControllers));

			if (toolbar == null)
				throw new ArgumentNullException (nameof (toolbar));

			allowedItemIdentifiers = clientSession.SessionKind == ClientSessionKind.LiveInspection
				? Identifiers.InspectorDefault
				: Identifiers.WorkbooksDefault;

			this.toolbar = toolbar;

			toolbar.Delegate = this;

			runAllItem = CreateButton (
				Identifiers.RunAll,
				"runAllSubmissions:",
				Catalog.GetString ("Run All"),
				Catalog.GetString ("Run the whole workbook from top to bottom"),
				"ToolbarRunTemplate");

			refreshItem = CreateButton (
				Identifiers.Refresh,
				"refreshVisualTree:",
				Catalog.GetString ("Refresh"),
				Catalog.GetString ("Refresh the application's visual tree in Inspector"),
				"ToolbarRefreshTemplate");

			inspectItem = CreateButton (
				Identifiers.Inspect,
				"inspectView:",
				Catalog.GetString ("Inspect"),
				Catalog.GetString ("Select a UI element to inspect in the running application"),
				"ToolbarInspectTemplate");

			var targetSelector = new WorkbookTargetSelector (
				clientSession.ViewControllers.WorkbookTargets) {
				Font = NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize)
			};

			var targetSelectorItemMenu = new NSMenuItem (Catalog.GetString ("Target"));
			targetSelectorItemMenu.Submenu = targetSelector.Menu;

			targetSelectorItem = new NSToolbarItem (Identifiers.TargetSelector) {
				View = targetSelector,
				MinSize = targetSelector.Frame.Size,
				MaxSize = targetSelector.Frame.Size,
				VisibilityPriority = -1000,
				MenuFormRepresentation = targetSelectorItemMenu
			};

			targetSelector.Activated += (sender, e) => {
				var size = targetSelector.GetToolbarSize ();
				targetSelectorItem.MinSize = size;
				targetSelectorItem.MaxSize = size;
				centeringItem.UpdateWidth ();
				viewControllers.WindowTabs.SelectedTabViewItemIndex = 0;
			};

			centeringItem = new CenteringToolbarItem (Identifiers.CenteringSpacer);

			statusItem = new StatusToolbarItem {
				View = viewControllers.Status.View
			};

			tabViewItem = new NSToolbarItem (Identifiers.TabView) {
				PaletteLabel = Catalog.GetString ("Views")
			};

			tabViewItem.View = viewControllers.WindowTabs.ToolbarSegmentedControl;

			viewControllers.WindowTabs.ItemSelected += (sender, e) => {
				switch (viewControllers.WindowTabs.SelectedTabViewItemIndex) {
				case 0:
					RemoveItem (Identifiers.Inspect);
					RemoveItem (Identifiers.Refresh);
					if (clientSession.SessionKind != ClientSessionKind.LiveInspection)
						toolbar.InsertItem (Identifiers.RunAll, 0);
					clientSession.WorkbookPageView.DelayNewCodeCellFocus = false;
					break;
				case 1:
					RemoveItem (Identifiers.RunAll);
					toolbar.InsertItem (Identifiers.Inspect, 0);
					toolbar.InsertItem (Identifiers.Refresh, 0);
					clientSession.WorkbookPageView.DelayNewCodeCellFocus = true;
					break;
				}

				centeringItem.UpdateWidth ();
			};

			NSWindow.Notifications.ObserveDidResize ((sender, e) => {
				statusItem.UpdateSize (e.Notification.Object as NSWindow);
				centeringItem.UpdateWidth ();
			});
		}

		void RemoveItem (string identifier)
		{
			var items = toolbar.Items;
			for (int i = 0; i < items.Length; i++) {
				if (items [i].Identifier == identifier) {
					toolbar.RemoveItem (i);
					return;
				}
			}
		}

		public override string [] AllowedItemIdentifiers (NSToolbar toolbar)
			=> allowedItemIdentifiers;

		public override string [] DefaultItemIdentifiers (NSToolbar toolbar)
			=> allowedItemIdentifiers;

		public override NSToolbarItem WillInsertItem (
			NSToolbar toolbar,
			string itemIdentifier,
			bool willBeInserted)
		{
			foreach (var item in toolbar.Items) {
				if (item.Identifier == itemIdentifier)
					return null;
			}

			switch (itemIdentifier) {
			case Identifiers.RunAll:
				return runAllItem;
			case Identifiers.TargetSelector:
				return targetSelectorItem;
			case Identifiers.CenteringSpacer:
				return centeringItem;
			case Identifiers.Status:
				return statusItem;
			case Identifiers.TabView:
				return tabViewItem;
			case Identifiers.Refresh:
				return refreshItem;
			case Identifiers.Inspect:
				return inspectItem;
			}

			return null;
		}

		NSToolbarItem CreateButton (
			string identifier,
			string selector,
			string label,
			string tooltip,
			string imageName)
		{
			var button = new NSButton {
				BezelStyle = NSBezelStyle.TexturedRounded,
				Image = NSImage.ImageNamed (imageName),
				Action = new ObjCRuntime.Selector (selector)
			};

			button.SetFrameSize (new CGSize (37, 25));

			var item = new NSControlToolbarItem (identifier, button) {
				Label = label,
				PaletteLabel = label,
				ToolTip = tooltip,
				MinSize = button.Frame.Size,
				MaxSize = button.Frame.Size
			};

			return item;
		}

		sealed class StatusToolbarItem : NSToolbarItem
		{
			StatusToolbarItem (IntPtr handle) : base (handle)
			{
			}

			public StatusToolbarItem () : base (Identifiers.Status)
			{
				Label = "Status";
				PaletteLabel = "Status";
			}

			public void UpdateSize (NSWindow window)
			{
				if (window.Toolbar != Toolbar)
					return;

				nfloat width = 100;
				nfloat height = 25;

				width = NMath.Max (width, window.Frame.Width * 0.4f);
				var size = new CGSize (NMath.Max (0, width), height);

				MaxSize = size;
				MinSize = size;
			}
		}

		sealed class NSControlToolbarItem : NSToolbarItem
		{
			NSControlToolbarItem (IntPtr handle) : base (handle)
			{
			}

			public new NSControl View {
				get { return (NSControl)base.View; }
			}

			public override bool Enabled {
				get { return View.Enabled; }
				set { View.Enabled = value; }
			}

			public NSControlToolbarItem (string identifier, NSControl control) : base (identifier)
			{
				if (control == null)
					throw new ArgumentNullException (nameof (control));

				base.View = control;
			}

			public override void Validate ()
			{
				NSResponder responder = NSApplication.SharedApplication.KeyWindow;

				while (responder != null) {
					if (responder.RespondsToSelector (Action)) {
						Enabled = true;
						return;
					}

					responder = responder.NextResponder;
				}

				Enabled = false;
			}
		}
	}
}