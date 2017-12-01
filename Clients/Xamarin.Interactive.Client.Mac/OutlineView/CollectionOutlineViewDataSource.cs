//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using AppKit;
using Foundation;
using ObjCRuntime;

using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.OutlineView
{
    sealed class CollectionOutlineViewDataSource : NSOutlineViewDataSource
    {
        public struct ReloadEventArgs
        {
            public NodeProxy Item { get; }
            public bool ReloadChildren { get; }

            public ReloadEventArgs (NodeProxy item, bool reloadChildren)
            {
                if (item == null)
                    throw new ArgumentNullException (nameof (item));

                Item = item;
                ReloadChildren = reloadChildren;
            }
        }

        public sealed class NodeProxy : NSObject
        {
            const string RenameSelectorName = "rename:";

            public static readonly Selector RenameSelector = new Selector (RenameSelectorName);

            public TreeNode Node { get; }

            public NodeProxy (TreeNode node)
            {
                if (node == null)
                    throw new ArgumentNullException (nameof (node));

                Node = node;
            }

            [Export (RenameSelectorName)]
            void Rename (NSObject sender)
            {
                if (Node.IsRenamable)
                    Node.Name = ((NSTextField)sender).StringValue;
            }
        }

        readonly NodeProxy root;
        readonly Dictionary<TreeNode, NodeProxy> nodeCache = new Dictionary<TreeNode, NodeProxy> ();
        readonly Dictionary<object, TreeNode> childrenToParent = new Dictionary<object, TreeNode> ();

        public event EventHandler<ReloadEventArgs> Reload;

        public CollectionOutlineViewDataSource (TreeNode root)
        {
            if (root == null)
                throw new ArgumentNullException (nameof (root));

            this.root = BindOutlineViewNode (root);
        }

        public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
        {
            if (item == null)
                return 1;

            var children = (item as NodeProxy)?.Node?.Children;
            if (children != null)
                return children.Count;

            return 0;
        }

        public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
            => GetChildrenCount (outlineView, item) > 0;

        public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
        {
            if (item == null)
                return root;

            var node = (item as NodeProxy)?.Node;
            if (node != null) {
                var child = node.Children [(int)childIndex];
                return BindOutlineViewNode (child);
            }

            return null;
        }

        public bool TryGetProxyNode (TreeNode node, out NodeProxy proxy)
        {
            if (node == null) {
                proxy = null;
                return false;
            }

            return nodeCache.TryGetValue (node, out proxy);
        }

        NodeProxy BindOutlineViewNode (TreeNode node)
        {
            if (node == null)
                return null;

            NodeProxy nsNode;

            if (!TryGetProxyNode (node, out nsNode)) {
                nsNode = new NodeProxy (node);
                nodeCache.Add (node, nsNode);
                StartMonitoringNode (node);
            }

            return nsNode;
        }

        void NotifyReload (NodeProxy proxy, bool reloadChildren)
        {
            if (proxy != null)
                Reload?.Invoke (this, new ReloadEventArgs (proxy, reloadChildren));
        }

        void StartMonitoringNode (TreeNode node)
        {
            var propertyChanged = node as INotifyPropertyChanged;
            if (propertyChanged != null)
                propertyChanged.PropertyChanged += HandlePropertyChanged;

            var propertyChanging = node as INotifyPropertyChanging;
            if (propertyChanging != null)
                propertyChanging.PropertyChanging += HandlePropertyChanging;

            StartMonitoringNodeChildren (node);
        }

        void StopMonitoringNode (TreeNode node, bool expungeProxy = true)
        {
            var propertyChanged = node as INotifyPropertyChanged;
            if (propertyChanged != null)
                propertyChanged.PropertyChanged -= HandlePropertyChanged;

            var propertyChanging = node as INotifyPropertyChanging;
            if (propertyChanging != null)
                propertyChanging.PropertyChanging -= HandlePropertyChanging;

            StopMonitoringNodeChildren (node);

            if (expungeProxy)
                nodeCache.Remove (node);
        }

        void StartMonitoringNodeChildren (TreeNode node)
        {
            var children = node?.Children;
            if (children == null)
                return;

            var collectionChanged = children as INotifyCollectionChanged;
            if (collectionChanged != null)
                collectionChanged.CollectionChanged += HandleCollectionChanged;

            childrenToParent [children] = node;
        }

        void StopMonitoringNodeChildren (TreeNode node)
        {
            var children = node?.Children;
            if (children == null)
                return;

            var collectionChanged = children as INotifyCollectionChanged;
            if (collectionChanged != null)
                collectionChanged.CollectionChanged -= HandleCollectionChanged;

            childrenToParent.Remove (children);
        }

        void HandlePropertyChanging (object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == "Children")
                StopMonitoringNodeChildren (sender as TreeNode);
        }

        void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            var node = sender as TreeNode;
            if (node == null)
                return;

            NodeProxy proxy;
            TryGetProxyNode (node, out proxy);

            if (e.PropertyName == "Children") {
                StartMonitoringNodeChildren (node);
                NotifyReload (proxy, true);
            } else
                NotifyReload (proxy, false);
        }

        void HandleCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) {
                foreach (TreeNode oldNode in e.OldItems)
                    StopMonitoringNode (oldNode);
            }

            if (e.NewItems != null) {
                foreach (TreeNode newNode in e.NewItems)
                    StartMonitoringNode (newNode);
            }

            NodeProxy proxy;
            if (TryGetProxyNode (childrenToParent [sender], out proxy))
                NotifyReload (proxy, true);
        }
    }
}