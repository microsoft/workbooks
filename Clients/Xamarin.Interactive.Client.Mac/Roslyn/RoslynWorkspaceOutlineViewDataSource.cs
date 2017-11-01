//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Foundation;
using AppKit;

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.Client.Mac.Roslyn
{
    sealed class RoslynModel
    {
        public abstract class Node : NSObject
        {
            public virtual int GetChildrenCount () => 0;
            public virtual bool CanExpand () => false;
            public virtual Node GetChild (int index)
            {
                throw new ArgumentOutOfRangeException (nameof (index));
            }
        }

        public abstract class EnumerableChildrenNode<TChildNode> : Node where TChildNode : Node
        {
            protected abstract IEnumerable<TChildNode> GetChildren ();

            public sealed override int GetChildrenCount () => GetChildren ().Count ();
            public sealed override bool CanExpand () => GetChildren ().Any ();
            public sealed override Node GetChild (int index) => GetChildren ().ElementAt (index);
        }

        public class SolutionNode : EnumerableChildrenNode<ProjectNode>
        {
            public static readonly SolutionNode Empty = new SolutionNode ();

            readonly ImmutableArray<ProjectNode> projects;

            public SolutionId SolutionId { get; }

            SolutionNode ()
            {
                projects = ImmutableArray<ProjectNode>.Empty;
            }

            public SolutionNode (Solution solution)
            {
                SolutionId = solution.Id;
                projects = solution
                    .GetProjectDependencyGraph ()
                    .GetTopologicallySortedProjects ()
                    .Select (id => new ProjectNode (solution.GetProject (id)))
                    .ToImmutableArray ();
            }

            protected override IEnumerable<ProjectNode> GetChildren () => projects;
        }

        public class ProjectNode : EnumerableChildrenNode<Node>
        {
            readonly ImmutableArray<Node> children;

            public ProjectId ProjectId { get; }

            public ProjectNode (Project project)
            {
                ProjectId = project.Id;

                children = ImmutableArray<Node>.Empty;

                var projectReferences = new ProjectReferencesFolderNode (project);
                if (projectReferences.CanExpand ())
                    children = children.Add (projectReferences);

                var metadataReferences = new MetadataReferencesFolderNode (project);
                if (metadataReferences.CanExpand ())
                    children = children.Add (metadataReferences);
            }

            protected override IEnumerable<Node> GetChildren () => children;
        }

        public class ProjectReferencesFolderNode : EnumerableChildrenNode<ProjectReferenceNode>
        {
            readonly ImmutableArray<ProjectReferenceNode> children;

            public ProjectReferencesFolderNode (Project project)
            {
                children = project
                    .ProjectReferences
                    .Select (r => new ProjectReferenceNode (project.Id, r.ProjectId))
                    .ToImmutableArray ();
            }

            protected override IEnumerable<ProjectReferenceNode> GetChildren () => children;
        }

        public class ProjectReferenceNode : Node
        {
            public ProjectId ProjectId { get; }
            public ProjectId ReferenceId { get; }

            public ProjectReferenceNode (ProjectId projectId, ProjectId referenceId)
            {
                ProjectId = projectId;
                ReferenceId = referenceId;
            }
        }

        public class MetadataReferencesFolderNode : EnumerableChildrenNode<MetadataReferenceNode>
        {
            readonly ImmutableArray<MetadataReferenceNode> children;

            public MetadataReferencesFolderNode (Project project)
            {
                children = project
                    .MetadataReferences
                    .Select (r => new MetadataReferenceNode (r))
                    .ToImmutableArray ();
            }

            protected override IEnumerable<MetadataReferenceNode> GetChildren () => children;
        }

        public class MetadataReferenceNode : Node
        {
            public MetadataReference Reference { get; }

            public MetadataReferenceNode (MetadataReference reference)
            {
                Reference = reference;
            }
        }
    }

    class RoslynWorkspaceOutlineViewDataSource : NSOutlineViewDataSource
    {
        RoslynModel.SolutionNode solutionNode = RoslynModel.SolutionNode.Empty;

        public void ReloadSolution (Solution solution)
        {
            solutionNode = solution == null
                ? RoslynModel.SolutionNode.Empty
                : new RoslynModel.SolutionNode (solution);
        }

        public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
            => ((RoslynModel.Node)item ?? solutionNode).GetChildrenCount ();

        public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
            => ((RoslynModel.Node)item ?? solutionNode).CanExpand ();

        public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
            => ((RoslynModel.Node)item ?? solutionNode).GetChild ((int)childIndex);

        public override NSObject GetObjectValue (NSOutlineView outlineView, NSTableColumn tableColumn,
            NSObject item) => item;
    }
}