//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac.Roslyn
{
    sealed class RoslynWorkspaceOutlineViewDelegate : NSOutlineViewDelegate
    {
        readonly RoslynWorkspaceExplorerWindowController windowController;

        public RoslynWorkspaceOutlineViewDelegate (RoslynWorkspaceExplorerWindowController windowController)
        {
            this.windowController = windowController;
        }

        public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
        {
            var node = (RoslynModel.Node)item;
            var projectNode = node as RoslynModel.ProjectNode;
            var projectReferenceNode = node as RoslynModel.ProjectReferenceNode;
            var metadataReferenceNode = node as RoslynModel.MetadataReferenceNode;

            var workspace = windowController.Workspace;

            var view = (NSTableCellView)outlineView.MakeView (tableColumn.Identifier, this);

            string name = null;
            string details = null;

            if (projectNode != null) {
                name = workspace.CurrentSolution.GetProject (projectNode.ProjectId).Name;
                details = projectNode.ProjectId.Id.ToString ();
            } else if (projectReferenceNode != null) {
                var referenceId = projectReferenceNode.ReferenceId;
                name = workspace.CurrentSolution.GetProject (referenceId).Name;
                details = projectReferenceNode.ReferenceId.Id.ToString ();
            } else if (metadataReferenceNode != null) {
                details = metadataReferenceNode.Reference.Display;
                name = Path.GetFileNameWithoutExtension (details);
            } else if (node is RoslynModel.ProjectReferencesFolderNode)
                name = "Project References";
            else if (node is RoslynModel.MetadataReferencesFolderNode)
                name = "Metadata References";

            switch (tableColumn.Identifier) {
            case "name":
                view.TextField.StringValue = name ?? String.Empty;
                break;
            case "details":
                view.TextField.StringValue = details ?? String.Empty;
                break;
            }

            return view;
        }

        public override void SelectionDidChange (NSNotification notification)
        {
            var view = (NSOutlineView)notification.Object;
            var projectNode = view.ItemAtRow (view.SelectedRow) as RoslynModel.ProjectNode;
            if (projectNode != null)
                windowController.SelectProject (projectNode);
        }
    }
}