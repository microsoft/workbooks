//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

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

        internal TableOfContentsNode (bool withChildren)
        {
            IconName = "highlight";
            IsSelectable = true;

            if (withChildren)
                base.Children = new ObservableCollection<TableOfContentsNode> (EqualityComparer);
        }
    }
}