//
// MenuManager.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using AppKit;

using Xamarin.Interactive.Editor;

namespace Xamarin.Interactive.Client.Mac.Menu
{
	sealed class MenuManager
	{
		readonly NSMenu rootMenu;

		public MenuManager (NSMenu rootMenu)
		{
			if (rootMenu == null)
				throw new ArgumentNullException (nameof (rootMenu));

			this.rootMenu = rootMenu;
			Update (null);
		}

		public void Update (IEditor editor) => Update (rootMenu, null, editor);

		void Update (NSMenu menu, NSMenuItem parentItem, IEditor editor)
		{
			for (nint i = 0, n = menu.Count; i < n; i++) {
				var item = menu.ItemAt (i);
				if (item.HasSubmenu) {
					Update (item.Submenu, item, editor);
					item.Hidden = !HasVisibleMenuItems (item, true);
				} else
					(item as XIEditorMenuItem)?.Bind (editor);
			}
		}

		bool HasVisibleMenuItems (NSMenuItem item, bool root)
		{
			if (!root && (item.Hidden || item.IsSeparatorItem))
				return false;

			if (!item.HasSubmenu)
				return true;

			var menu = item.Submenu;

			for (nint i = 0, n = menu.Count; i < n; i++) {
				if (HasVisibleMenuItems (menu.ItemAt (i), false))
					return true;
			}

			return false;
		}
	}
}