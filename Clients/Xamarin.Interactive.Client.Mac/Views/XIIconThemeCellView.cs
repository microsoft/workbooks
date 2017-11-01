//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Client.Mac.Views
{
    [Register (nameof (XIIconThemeOutlineCellView))]
    sealed class XIIconThemeOutlineCellView : NSTableCellView
    {
        XIIconThemeOutlineCellView (IntPtr handle) : base (handle)
        {
        }

        public string IconName { get; set; }

        public override NSBackgroundStyle BackgroundStyle {
            get { return base.BackgroundStyle; }
            set {
                base.BackgroundStyle = value;
                ImageView.Image = Theme.Current.GetIcon (
                    IconName,
                    16,
                    value == NSBackgroundStyle.Dark);
            }
        }
    }
}