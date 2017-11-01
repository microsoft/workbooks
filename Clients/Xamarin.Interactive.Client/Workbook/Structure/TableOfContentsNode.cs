//
// TableOfContentsNode.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;

using Xamarin.Interactive.Collections;
using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Workbook.Structure
{
	sealed class TableOfContentsNode : TreeNode
	{
		sealed class _EqualityComparer : IEqualityComparer<TableOfContentsNode>
		{
			public bool Equals (TableOfContentsNode x, TableOfContentsNode y)
				=> x?.Id == y?.Id;

			public int GetHashCode (TableOfContentsNode obj)
				=> obj?.Id == null ? 0 : obj.Id.GetHashCode ();
		}

		static readonly IEqualityComparer<TableOfContentsNode> EqualityComparer = new _EqualityComparer ();

		public new ObservableCollection<TableOfContentsNode> Children
			=> (ObservableCollection<TableOfContentsNode>)base.Children;

		public TableOfContentsNode () : this (true)
		{
		}

		TableOfContentsNode (bool withChildren)
		{
			IconName = "highlight";
			IsSelectable = true;

			if (withChildren)
				base.Children = new ObservableCollection<TableOfContentsNode> (EqualityComparer);
		}

		public void RebuildFromJavaScript (dynamic headings)
		{
			var headingsCount = headings.length;
			if (headingsCount <= 0) {
				Children.Clear ();
				return;
			}

			// Build a complete list of the new headings, reusing nodes from
			// the existing headings model where possible (same old IDs).
			// This will ensure the "Become" call does as little work as
			// possible, simply diffing the two models and applying relevant
			// changes. In turn this allows the views to react nicely (e.g.
			// by preserving selections and expansion state).

			var updatedList = new List<TableOfContentsNode> (headingsCount);

			for (int i = 0; i < headingsCount; i++) {
				var heading = headings [i];
				var oldId = heading.oldId;

				var node = Children.FirstOrDefault (n => n.Id == oldId)
					?? new TableOfContentsNode (false);
				node.Id = heading.newId;
				node.Name = heading.text;

				updatedList.Add (node);
			}

			Children.UpdateTo (updatedList);
		}
	}
}