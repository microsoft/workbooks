//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.ComponentModel;

using AppKit;
using CoreGraphics;

using Xamarin.Interactive.Client.ViewControllers;

namespace Xamarin.Interactive.Client.Mac
{
    sealed class WorkbookTargetSelector : NSPopUpButton
    {
        readonly WorkbookTargetsViewController viewController;

        public WorkbookTargetSelector (WorkbookTargetsViewController viewController)
        {
            if (viewController == null)
                throw new ArgumentNullException (nameof (viewController));

            this.viewController = viewController;

            BezelStyle = NSBezelStyle.TexturedRounded;
            Title = String.Empty;
            IsSpringLoaded = true;

            Menu.AutoEnablesItems = false;
            Hidden = true;

            viewController.CollectionChanged += ViewController_CollectionChanged;
            viewController.PropertyChanged += ViewController_PropertyChanged;

            Activated += (sender, e) => {
                var index = (int)IndexOfSelectedItem;
                viewController.SelectedTarget = index < 0 ? null : viewController [index];
            };
        }

        void ViewController_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action) {
            case NotifyCollectionChangedAction.Reset:
                Hidden = true;
                RemoveAllItems ();
                break;
            case NotifyCollectionChangedAction.Add:
                foreach (WorkbookAppViewController item in e.NewItems) {
                    if (item == WorkbookAppViewController.SeparatorItem) {
                        Menu.AddItem (NSMenuItem.SeparatorItem);
                        continue;
                    }

                    var menuItem = new NSMenuItem (item.Label) {
                        Image = Theme.Current.GetIcon (item.Icon, 16),
                        Enabled = item.Enabled
                    };

                    Menu.AddItem (menuItem);
                }
                break;
            default:
                throw new NotSupportedException ($"{e.Action}");
            }
        }

        void ViewController_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof (viewController.SelectedTarget)) {
                var selectedTarget = viewController.SelectedTarget;

                if (selectedTarget == null) {
                    Hidden = true;
                    SelectItem (null as NSMenuItem);
                } else {
                    Hidden = false;
                    SelectItem (viewController.IndexOf (selectedTarget));
                }

                SendAction (Action, Target);
            }
        }

        public CGSize GetToolbarSize ()
        {
            var selected = SelectedItem;
            if (selected == null)
                return Frame.Size;

            var cell = new NSMenuItemCell {
                MenuItem = selected
            };

            cell.CalcSize ();

            return new CGSize (
                cell.CellSize.Width - cell.StateImageWidth () - cell.KeyEquivalentWidth,
                cell.CellSize.Height);
        }
    }
}