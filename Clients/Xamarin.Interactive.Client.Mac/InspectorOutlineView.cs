//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    [Register ("InspectorOutlineView")]
    sealed class InspectorOutlineView : NSOutlineView
    {
        public InspectorOutlineView (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public InspectorOutlineView (NSCoder coder) : base (coder)
        {
        }

        bool BuildParentChain (Stack<NSObject> stack, NSObject parent, NSObject targetItem)
        {
            stack.Push (parent);

            if (parent == targetItem)
                return true;

            for (nint i = 0, n = DataSource.GetChildrenCount (this, parent); i < n; i++) {
                var child = DataSource.GetChild (this, i, parent);
                if (child == null)
                    continue;

                if (child == targetItem || BuildParentChain (stack, child, targetItem))
                    return true;
            }

            stack.Pop ();
            return false;
        }

        public void SelectItem (NSObject item)
        {
            if (item == null || DataSource == null)
                return;

            var root = (InspectViewPeer)((InspectViewDataSource)DataSource).Root.Peer;
            if (root == null)
                return;

            var parents = new Stack<NSObject> ();
            BuildParentChain (parents, root, item);
            foreach (var parent in parents.Reverse ())
                ExpandItem (parent);

            var row = RowForItem (item);
            if (row >= 0) {
                SelectRow (row, false);
                ScrollRowToVisible (row);
            }
        }
    }
}