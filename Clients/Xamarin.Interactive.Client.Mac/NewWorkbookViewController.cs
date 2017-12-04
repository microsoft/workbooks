//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Linq;

using AppKit;
using Foundation;

using Xamarin.Interactive.Client.ViewControllers;

using SharedViewController = Xamarin.Interactive.Client.ViewControllers.NewWorkbookViewController;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class NewWorkbookViewController : NSViewController, INSCollectionViewDelegate
    {
        SharedViewController viewController;

        NewWorkbookViewController (IntPtr handle) : base (handle)
        {
        }

        public AgentType SelectedAgentType {
            get => viewController.SelectedAgentType;
            set => viewController.SelectedAgentType = value;
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            viewController = new SharedViewController ();
            viewController.PropertyChanged += ViewController_PropertyChanged;

            frameworkCollectionView.DataSource = new DataSource (this);
            frameworkCollectionView.Delegate = this;

            frameworkPopupButton.Activated += (sender, e)
                => viewController.SelectedItem.SelectedWorkbookApp =
                    viewController.SelectedItem.WorkbookApps [
                        (int)frameworkPopupButton.IndexOfSelectedItem];

            Bind (viewController.SelectedItem);
        }

        public override void ViewWillAppear ()
        {
            base.ViewWillAppear ();

            var window = View?.Window;
            if (window == null)
                return;

            if (window.IsSheet) {
                cancelButton.Hidden = false;
                return;
            }

            window.TitlebarAppearsTransparent = true;
            window.TitleVisibility = NSWindowTitleVisibility.Hidden;

            window.StyleMask |= NSWindowStyle.FullSizeContentView;
            window.StyleMask &= ~(NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable);

            window.StandardWindowButton (NSWindowButton.MiniaturizeButton).Hidden = true;
            window.StandardWindowButton (NSWindowButton.ZoomButton).Hidden = true;

            cancelButton.Hidden = !View.Window.IsSheet;
        }

        void Bind (NewWorkbookItem item)
        {
            var index = item == null
                ? -1
                : viewController.Items.IndexOf (item);

            featuresStackView
                .Subviews
                .Where (v => v != frameworkPopupButton)
                .ForEach (v => {
                    v.RemoveFromSuperview ();
                    v.Dispose ();
                });

            if (index < 0) {
                frameworkCollectionView.SelectionIndexes = new NSIndexSet ();
                frameworkPopupButton.Hidden = true;
                frameworkPopupButton.Menu = null;
            } else {
                frameworkCollectionView.SelectionIndexes = new NSIndexSet (index);
                frameworkPopupButton.Hidden = item.WorkbookApps.Count < 2;
                frameworkPopupButton.Menu = new NSMenu ();

                foreach (var workbookApp in item.WorkbookApps) {
                    var menuItem = new NSMenuItem (workbookApp.Label);
                    frameworkPopupButton.Menu.AddItem (menuItem);
                    if (item.SelectedWorkbookApp == workbookApp)
                        frameworkPopupButton.SelectItem (menuItem);
                }

                foreach (var feature in item.SelectedWorkbookApp.OptionalFeatures) {
                    var checkButton = new NSButton {
                        Font = NSFont.ControlContentFontOfSize (NSFont.SmallSystemFontSize),
                        Title = feature.Label,
                        ToolTip = feature.Description,
                        State = feature.Enabled
                            ? NSCellStateValue.On
                            : NSCellStateValue.Off
                    };

                    checkButton.SetButtonType (NSButtonType.Switch);

                    checkButton.Activated += (sender, e)
                        => feature.Enabled = checkButton.State == NSCellStateValue.On;

                    featuresStackView.AddView (checkButton, NSStackViewGravity.Bottom);
                }
            }
        }

        void ViewController_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof (SharedViewController.SelectedItem))
                Bind (viewController.SelectedItem);
        }

        [Export ("collectionView:didSelectItemsAtIndexPaths:")]
        void ItemsSelected (NSCollectionView collectionView, NSSet indexPaths)
        {
            var index = (int)indexPaths.ToArray<NSIndexPath> () [0].Item;
            viewController.SelectedItem = viewController.Items [index];
        }

        [Export ("collectionItemViewDoubleClick:")]
        void CollectionItemViewDoubleClick (NSObject sender)
            => CreateWorkbook (sender);

        [Export ("createWorkbook:")]
        void CreateWorkbook (NSObject sender)
        {
            viewController.SaveLastCreatedWorkbookPreference ();
            SessionDocumentController.SharedDocumentController.OpenDocument (
                viewController.SelectedItem.CreateClientSessionUri ());
            ((NewWorkbookWindow)View.Window).Close (sender);
        }

        sealed class DataSource : NSCollectionViewDataSource
        {
            readonly NewWorkbookViewController viewController;

            public DataSource (NewWorkbookViewController viewController)
            {
                this.viewController = viewController;
            }

            public override nint GetNumberofItems (NSCollectionView collectionView, nint section)
                => viewController.viewController.Items.Count;

            public override NSCollectionViewItem GetItem (
                NSCollectionView collectionView,
                NSIndexPath indexPath)
                => ((NewWorkbookPrimaryChoiceItem)collectionView.MakeItem (
                    nameof (NewWorkbookPrimaryChoiceItem),
                    indexPath)).Bind (
                        viewController.viewController.Items [(int)indexPath.Item]);
        }
    }
}