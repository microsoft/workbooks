//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Wpf
{
    /// <summary>
	/// An artificial "root" element containing InspectViews for all of the app's current Windows and their
	/// children.
	/// </summary>
    [Serializable]
    class WpfRootInspectView : InspectView
    {
        public WpfRootInspectView ()
        {
            SetHandle (IntPtr.Zero);
            DisplayName = "Root";

            foreach (Window window in Application.Current.Windows) {
                if (window.IsVisible)
                    AddSubview (new WpfInspectView (window));
            }
        }

        protected override void UpdateCapturedImage ()
        {
            // TODO
        }
    }
}