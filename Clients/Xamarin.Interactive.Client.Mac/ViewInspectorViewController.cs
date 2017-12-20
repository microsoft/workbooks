//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

using Xamarin.Interactive.Client.ViewInspector;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Mac
{
    abstract class ViewInspectorViewController : SessionViewController
    {
        protected ViewInspectorViewController (IntPtr handle) : base (handle)
        {
        }

        public new ViewInspectorMainViewController ParentViewController
            => (ViewInspectorMainViewController)base.ParentViewController;

        public InspectView RepresentedView => tree?.RepresentedNode.View;

        public InspectView SelectedView => tree?.SelectedNode?.View;

        public InspectView RootView => tree?.RootNode?.View;

        InspectTreeRoot tree;
        public InspectTreeRoot Tree {
            get => tree;
            set {
                if (value == tree)
                    return;

                void HandleTreeEvent (object sender, PropertyChangedEventArgs args)
                {
                    var treeSensed = sender as InspectTreeRoot;
                    switch (args.PropertyName) {
                    case nameof (InspectTreeRoot.RepresentedNode):
                        OnRepresentedNodeChanged ();
                        break;
                    case nameof (InspectTreeRoot.RootNode):
                        OnRootNodeChanged ();
                        break;
                    case nameof (InspectTreeRoot.SelectedNode):
                        OnSelectedNodeChanged ();
                        break;
                    }
                }

                if (tree != null)
                    ((INotifyPropertyChanged)tree).PropertyChanged -= HandleTreeEvent;

                tree = value;

                if (tree != null)
                    ((INotifyPropertyChanged)tree).PropertyChanged += HandleTreeEvent;

                OnTreeChanged ();
            }
        }

        protected virtual void OnRepresentedViewChanged ()
        {
        }

        protected virtual void OnSelectedViewChanged ()
        {
        }

        protected virtual void OnRootViewChanged ()
        {
        }

        protected virtual void OnTreeChanged ()
        {
            OnRootNodeChanged ();
            OnRepresentedNodeChanged ();
            OnSelectedNodeChanged ();
        }

        protected virtual void OnRepresentedNodeChanged ()
            => OnRepresentedViewChanged ();

        protected virtual void OnRootNodeChanged ()
            => OnRootViewChanged ();

        protected virtual void OnSelectedNodeChanged ()
            => OnSelectedViewChanged ();
    }
}