//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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