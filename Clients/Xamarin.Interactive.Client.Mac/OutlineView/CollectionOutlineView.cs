//
// CollectionOutlineView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.OutlineView
{
	[Register (nameof (CollectionOutlineView))]
	class CollectionOutlineView : NSOutlineView
	{
		sealed class ContextMenu : NSMenu
		{
			readonly CollectionOutlineView outlineView;

			public ContextMenu (CollectionOutlineView outlineView)
			{
				this.outlineView = outlineView;
				Delegate = new ContextMenuDelegate ();
			}

			sealed class ContextMenuDelegate : NSMenuDelegate
			{
				public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
				{
				}

				public override void NeedsUpdate (NSMenu menu)
				{
					var outlineView = ((ContextMenu)menu).outlineView;

					menu.RemoveAllItems ();

					var nodeProxy = outlineView.GetNodeProxy (outlineView.ClickedRow);
					if (nodeProxy?.Node == null)
						return;

					var items = outlineView.Delegate?.ContextMenuItemsForNode (nodeProxy.Node);
					if (items == null)
						return;

					foreach (var item in items)
						menu.AddItem (item);
				}
			}
		}

		readonly ContextMenu contextMenu;

		CollectionOutlineView (IntPtr handle) : base (handle)
		{
			contextMenu = new ContextMenu (this);
		}

		public override void ReloadData ()
		{
			base.ReloadData ();

			for (int i = 0; i < RowCount; i++)
				UpdateViewState (ItemAtRow (i));
		}

		public override void ReloadItem (NSObject item, bool reloadChildren)
		{
			base.ReloadItem (item, reloadChildren);
			UpdateViewState (item);
		}

		void UpdateViewState (NSObject item)
		{
			var nodeProxy = item as CollectionOutlineViewDataSource.NodeProxy;
			if (nodeProxy == null)
				return;

			var node = nodeProxy.Node;
			if (node == null)
				return;
			
			if (node.IsExpanded)
				ExpandItem (item, false);
			else
				CollapseItem (item, false);

			if (node.IsSelected)
				SelectedNodeProxy = nodeProxy;
			else if (SelectedNodeProxy == nodeProxy)
				SelectedNodeProxy = null;
		}

		public override NSMenu Menu {
			get { return contextMenu; }
			set { }
		}

		public new CollectionOutlineViewDelegate Delegate {
			get { return (CollectionOutlineViewDelegate)base.Delegate; }
			set { base.Delegate = value; }
		}

		public new CollectionOutlineViewDataSource DataSource {
			get { return (CollectionOutlineViewDataSource)base.DataSource; }
			set { base.DataSource = value; }
		}

		public CollectionOutlineViewDataSource.NodeProxy GetNodeProxy (nint row)
		{
			if (row >= 0)
				return ItemAtRow (row) as CollectionOutlineViewDataSource.NodeProxy;
			return null;
		}

		public CollectionOutlineViewDataSource.NodeProxy SelectedNodeProxy {
			get { return GetNodeProxy (SelectedRow); }
			set {
				nint row = -1;
				if (value != null)
					row = RowForItem (value);
				SelectRow (row, false);
			}
		}

		public TreeNode SelectedNode {
			get { return SelectedNodeProxy?.Node; }
			set {
				CollectionOutlineViewDataSource.NodeProxy proxy;
				DataSource.TryGetProxyNode (value, out proxy);
				SelectedNodeProxy = proxy;
			}
		}
	}
}