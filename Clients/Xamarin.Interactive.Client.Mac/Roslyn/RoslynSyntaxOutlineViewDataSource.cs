//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac.Roslyn
{
    using Microsoft.CodeAnalysis;

    sealed class RoslynSyntaxOutlineViewDataSource : NSOutlineViewDataSource
    {
        public class SyntaxNodeWrapper : NSObject
        {
            public SyntaxNode SyntaxNode { get; }

            public SyntaxNodeWrapper (SyntaxNode syntaxNode)
            {
                SyntaxNode = syntaxNode;
            }
        }

        readonly Compilation compilation;

        public RoslynSyntaxOutlineViewDataSource (Compilation compilation)
        {
            this.compilation = compilation;
        }

        public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
            => item == null
                ? compilation.SyntaxTrees.Count ()
                : ((SyntaxNodeWrapper)item).SyntaxNode.ChildNodes ().Count ();

        public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
            => ((SyntaxNodeWrapper)item).SyntaxNode.ChildNodes ().Any ();

        public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
            => item == null
                ? new SyntaxNodeWrapper (
                    compilation.SyntaxTrees.ElementAt ((int)childIndex).GetRoot ())
                : new SyntaxNodeWrapper (
                    ((SyntaxNodeWrapper)item).SyntaxNode.ChildNodes ().ElementAt ((int)childIndex));

        public override NSObject GetObjectValue (NSOutlineView outlineView, NSTableColumn tableColumn,
            NSObject item) => item;
    }
}