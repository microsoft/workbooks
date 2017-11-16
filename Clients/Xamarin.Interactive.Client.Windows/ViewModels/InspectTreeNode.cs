//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Xamarin.Interactive.Client.Windows.Views;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Client.Windows.ViewModels
{
    class InspectTreeNode : TreeNode
    {
        public InspectView View =>
            RepresentedObject as InspectView;

        public InspectTreeNode Parent { get; }

        string GetDisplayName (bool showBounds = false)
        {
            if (View == null)
                return String.Empty;

            var view = View;
            var boundsDisplay = String.Empty;

            if (showBounds) {
                var size = new Size (0, 0);
                if (false || view?.CapturedImage != null) {
                    var bitmap = new BitmapImage ();
                    bitmap.BeginInit ();

                    bitmap.StreamSource = new MemoryStream (view.CapturedImage);
                    bitmap.EndInit ();

                    size = new Size ((int)bitmap.Width, (int)bitmap.Height);
                }
                boundsDisplay = $"({view.X}, {view.Y}, {view.Width}, {view.Height}) - ({size.Width}, {size.Height})";
            }

            if (!String.IsNullOrEmpty (view.DisplayName))
                return view.DisplayName + boundsDisplay;

            var text = view.Type;

            var ofs = text.IndexOf ('.');
            if (ofs > 0) {
                switch (text.Substring (0, ofs)) {
                case "AppKit":
                case "SceneKit":
                case "WebKit":
                case "UIKit":
                    text = text.Substring (ofs + 1);
                    break;
                }
            }

            if (!String.IsNullOrEmpty (view.Description))
                text += $" — “{view.Description}”";

            text += boundsDisplay;
            return text;
        }

        public InspectTreeNode (InspectTreeNode parent, InspectView view)
        {
            Parent = parent;
            RepresentedObject = view;
            Name = GetDisplayName ();
            Children = view.Children.Select (c => new InspectTreeNode (this, c)).ToList ();
            IsRenamable = false;
            IsEditing = false;
            IsSelectable = !view.IsFakeRoot;
            ToolTip = view.Description;
            IsExpanded = true;
        }

        public IInspectTreeModel3D<T> Build3D<T> (IInspectTreeModel3D<T> node3D, TreeState state)
        {
            node3D.BuildPrimaryPlane (state);
            state.PushGeneration ();
            foreach (var child in GetRenderedChildren (state)) {
                var child3d = node3D.BuildChild (child, state);
                node3D.Add (child3d);
            }
            state.PopGeneration ();
            return node3D;
        }

        IEnumerable<InspectTreeNode> GetRenderedChildren (TreeState state, bool collapseLayers = true)
        {
            // exploit the fact that Layers can't have Subview or Layer set
            // to walk the children in collapsed layer format
            var layer = View.Layer;
            var children = Children.OfType<InspectTreeNode>();

            foreach (var child in children) {
                if (collapseLayers && child.View == layer)
                    children = child.Children.OfType<InspectTreeNode> ();
                else
                    yield return child;
            }
        }

        protected override void NotifyPropertyChanged ([CallerMemberName]string name = null)
        {
            base.NotifyPropertyChanged (name);
            switch (name) {
            case nameof (IsExpanded):
                break;
            }
        }
    }
}
