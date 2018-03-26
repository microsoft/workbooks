//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

using Foundation;
using AppKit;

using Xamarin.Interactive.CodeAnalysis.Roslyn;

namespace Xamarin.Interactive.Client.Mac.Roslyn
{
    using Microsoft.CodeAnalysis;

    sealed partial class RoslynWorkspaceExplorerWindowController : NSWindowController
    {
        SessionWindowController sessionWindowController;
        RoslynWorkspaceOutlineViewDataSource outlineViewDataSource;

        public Workspace Workspace { get; private set; }

        public RoslynWorkspaceExplorerWindowController (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public RoslynWorkspaceExplorerWindowController (NSCoder coder) : base (coder)
        {
        }

        public RoslynWorkspaceExplorerWindowController () : base ("RoslynWorkspaceExplorerWindow")
        {
        }

        public override void AwakeFromNib ()
        {
            outlineViewDataSource = new RoslynWorkspaceOutlineViewDataSource ();
            outlineView.DataSource = outlineViewDataSource;
            outlineView.Delegate = new RoslynWorkspaceOutlineViewDelegate (this);

            syntaxOutlineView.Delegate = new RoslynSyntaxOutlineViewDelegate ();

            NSWindow.Notifications.ObserveDidBecomeKey (
                (o, e) => AssociateWindow (e.Notification.Object));

            AssociateWindow (NSApplication.SharedApplication.MainWindow);

            base.AwakeFromNib ();
        }

        void AssociateWindow (NSObject window)
        {
            var controller = (window as SessionWindow)
                ?.WindowController as SessionWindowController;

            if (controller == null || controller == sessionWindowController)
                return;

            if (Workspace != null)
                Workspace.WorkspaceChanged -= HandleWorkspaceChanged;

            sessionWindowController = controller;

            var compilationWorkspace = SessionDocumentController.SharedDocumentController.DocumentForWindow (
                sessionWindowController?.Window)
                ?.Session
                ?.CompilationWorkspace;

            var workspaceField = compilationWorkspace
                ?.GetType ()
                .GetField (
                    "workspace",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            if (workspaceField != null && compilationWorkspace != null)
                Workspace = (Workspace)workspaceField.GetValue (compilationWorkspace);

            if (Workspace != null)
                Workspace.WorkspaceChanged += HandleWorkspaceChanged;

            RefreshModels ();
        }

        async void HandleWorkspaceChanged (object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.DocumentChanged)
                RefreshSyntaxTree (await e
                    .NewSolution
                    .GetProject (e.ProjectId)
                    .GetCompilationAsync ());
            else
                RefreshModels ();
        }

        void RefreshModels ()
        {
            outlineViewDataSource.ReloadSolution (Workspace?.CurrentSolution);
            outlineView.ReloadData ();
        }

        public void SelectProject (RoslynModel.ProjectNode projectNode)
            => RefreshSyntaxTree (Workspace
                .CurrentSolution
                .GetProject (projectNode.ProjectId)
                .GetCompilationAsync ()
                .Result);

        public void RefreshSyntaxTree (Compilation compilation)
        {
            syntaxOutlineView.DataSource = new RoslynSyntaxOutlineViewDataSource (compilation);
            syntaxOutlineView.ReloadData ();
            syntaxOutlineView.ExpandItem (null, true);
        }

        public new RoslynWorkspaceExplorerWindow Window => (RoslynWorkspaceExplorerWindow)base.Window;
    }
}