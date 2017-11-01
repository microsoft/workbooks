//
// ViewInspectorViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

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
            get { return representedView; }
            set {
                if (representedView == value)
                    return;

                representedView = value;
                OnRepresentedViewChanged ();
            }
        }

        InspectView selectedView;
        public InspectView SelectedView {
            get { return selectedView; }
            set {
                if (selectedView == value)
                    return;

                selectedView = value;
                OnSelectedViewChanged ();
            }
        }

        InspectView rootView;
        public InspectView RootView {
            get { return rootView; }
            set {
                if (rootView == value)
                    return;

                rootView = value;
                OnRootViewChanged ();
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
    }
}