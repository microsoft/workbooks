//
// RoslynSyntaxOutlineViewDelegate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac.Roslyn
{
    sealed class RoslynSyntaxOutlineViewDelegate : NSOutlineViewDelegate
    {
        public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
        {
            var view = (NSTableCellView)outlineView.MakeView (tableColumn.Identifier, this);
            var node = (RoslynSyntaxOutlineViewDataSource.SyntaxNodeWrapper)item;

            switch (tableColumn.Identifier) {
            case "type":
                view.TextField.StringValue = node.SyntaxNode.GetType ().Name;
                break;
            case "tostring":
                view.TextField.StringValue = node.SyntaxNode.ToString ();
                break;
            }

            return view;
        }
    }
}