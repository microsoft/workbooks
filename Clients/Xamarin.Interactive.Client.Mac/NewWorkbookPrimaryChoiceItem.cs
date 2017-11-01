//
// NewWorkbookPrimaryChoiceItem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Linq;

using AppKit;
using Foundation;
using ObjCRuntime;

using Xamarin.Interactive.Client.ViewControllers;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class NewWorkbookPrimaryChoiceItem : NSCollectionViewItem
    {
        NSImage image;
        NSImage selectedImage;

        public NewWorkbookPrimaryChoiceItem ()
            : base (nameof (NewWorkbookPrimaryChoiceItem), NSBundle.MainBundle)
        {
        }

        public override void AwakeFromNib ()
        {
            base.AwakeFromNib ();

            imageView = View.Subviews.OfType<NSImageView> ().First ();
            label = View.Subviews.OfType<NSTextField> ().First ();
        }

        public NewWorkbookPrimaryChoiceItem Bind (NewWorkbookItem item)
        {
            image = Theme.Current.GetIcon (item.IconName, 32, selected: false);
            selectedImage = Theme.Current.GetIcon (item.IconName, 32, selected: true);

            label.StringValue = item.Label;

            Update ();

            return this;
        }

        void Update ()
        {
            if (Selected) {
                imageView.Image = selectedImage;
                View.Layer.BackgroundColor = NSColor.KeyboardFocusIndicator.CGColor;
                label.TextColor = NSColor.SelectedMenuItemText;
            } else {
                imageView.Image = image;
                View.Layer.BackgroundColor = NSColor.Clear.CGColor;
                label.TextColor = NSColor.ControlText;
            }
        }

        public override bool Selected {
            get => base.Selected;
            set {
                base.Selected = value;
                Update ();
            }
        }

        [Register (nameof (NewWorkbookPrimaryChoiceItemView))]
        sealed class NewWorkbookPrimaryChoiceItemView : NSView
        {
            NewWorkbookPrimaryChoiceItemView (IntPtr handle) : base (handle)
            {
                WantsLayer = true;
                Layer.CornerRadius = 4;
            }

            public override void MouseDown (NSEvent theEvent)
            {
                base.MouseDown (theEvent);

                if (theEvent.ClickCount > 1)
                    NSApplication.SharedApplication.SendAction (
                        new Selector ("collectionItemViewDoubleClick:"),
                        null,
                        this);
            }
        }
    }
}