//
// PreferencesWindowController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using AppKit;
using CoreGraphics;
using Foundation;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Preferences
{
    sealed partial class PreferencesWindowController : NSWindowController
    {
        PreferencesWindowController (IntPtr handle) : base (handle)
        {
        }

        public new NSTabViewController ContentViewController
            => (NSTabViewController)base.ContentViewController;

        nfloat windowChromeHeight;
        nfloat maxIntrinsicContentViewWidth;

        public override void WindowDidLoad ()
        {
            base.WindowDidLoad ();

            if (WebKitPrefs.DeveloperExtrasEnabled)
                ContentViewController.AddTabViewItem (new NSTabViewItem {
                    Label = Catalog.GetString ("Developer"),
                    Image = NSImage.ImageNamed ("NSAdvanced"),
                    ViewController = NSStoryboard
                        .FromName ("Main", NSBundle.MainBundle)
                        .InstantiateController<PreferencesDeveloperViewController> ()
                });

            windowChromeHeight = Window.Frame.Size.Height - Window.ContentView.Frame.Size.Height;

            foreach (var item in ContentViewController.TabViewItems)
                maxIntrinsicContentViewWidth = NMath.Max (
                    maxIntrinsicContentViewWidth,
                    item.View.IntrinsicContentSize.Width);

            DidSelectTabViewItem (ContentViewController.TabView.Selected);

            Window.Center ();
        }

        public void DidSelectTabViewItem (NSTabViewItem item)
        {
            Window.Title = item.Label;
            ResizeWindowForView (item.View);
        }

        public void NotifyIntrinsicContentSizeUpdated (PreferencesView view)
        {
            if (ContentViewController.TabView.Selected.View == view)
                ResizeWindowForView (view);
        }

        void ResizeWindowForView (NSView view)
        {
            maxIntrinsicContentViewWidth = NMath.Max (
                maxIntrinsicContentViewWidth,
                view.IntrinsicContentSize.Width);

            var contentSize = view.IntrinsicContentSize;
            var newHeight = contentSize.Height + windowChromeHeight;
            var oldHeight = Window.Frame.Height;

            Window.SetFrame (
                new CGRect (
                    Window.Frame.X,
                    Window.Frame.Y + oldHeight - newHeight,
                    maxIntrinsicContentViewWidth,
                    newHeight),
                display: false,
                animate: true);
        }
    }
}