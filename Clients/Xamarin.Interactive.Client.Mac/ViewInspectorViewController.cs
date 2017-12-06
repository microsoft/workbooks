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

        InspectView representedView;
        public InspectView RepresentedView {
            get { return tree?.RepresentedNode.View ?? representedView; }
            set {
                if (representedView == value)
                    return;

                representedView = value;
                OnRepresentedViewChanged ();
            }
        }

        InspectView selectedView;
        public InspectView SelectedView {
            get { return tree?.SelectedNode?.View ?? selectedView; }
            set {
                if (selectedView == value)
                    return;

                selectedView = value;
                OnSelectedViewChanged ();
            }
        }

        InspectView rootView;
        public InspectView RootView {
            get { return tree?.RootNode?.View ?? rootView; }
            set {
                if (rootView == value)
                    return;

                rootView = value;

                if (tree == null)
                    OnRootViewChanged ();
            }
        }

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
        }

        protected virtual void OnRepresentedNodeChanged ()
        {
        }

        protected virtual void OnRootNodeChanged ()
        {
        }

        protected virtual void OnSelectedNodeChanged ()
        {
        }
    }
}