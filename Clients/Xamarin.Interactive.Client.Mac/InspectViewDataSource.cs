//
// InspectViewDataSource.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014 Xamarin Inc.

using System;

using Foundation;
using AppKit;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Mac
{
    sealed class InspectViewPeer : NSObject
    {
        public InspectView InspectView { get; }

        public InspectViewPeer (InspectView inspectView)
            => InspectView = inspectView;

        public static implicit operator InspectView (InspectViewPeer peer)
            => peer?.InspectView;
    }

    sealed class InspectViewDataSource : NSOutlineViewDataSource
    {
        static InspectViewDataSource ()
        {
            InspectView.PeerFactory = view => new InspectViewPeer (view);
        }

        public InspectView Root { get; private set; }

        public void Load (InspectView view)
        {
            Root = view;
        }

        public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
        {
            InspectView inspectView = (InspectViewPeer)item;

            if (inspectView == null)
                return false;

            var subviewsCount = inspectView.Subviews?.Count ?? 0;
            var layerCount = inspectView.Layer == null ? 0 : 1;
            var sublayerCount = inspectView.Sublayers?.Count ?? 0;

            return (subviewsCount + layerCount + sublayerCount) > 0;
        }

        public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
        {
            if (Root == null)
                return 0; // no tree exists

            InspectView view = (InspectViewPeer)item;
            if (view == null)
                return 1; // root node

            var childCount = 0;

            if (view.Subviews != null)
                childCount += view.Subviews.Count; // subviews

            if (view.Layer != null)
                childCount += 1; // layer

            if (view.Sublayers != null)
                childCount += view.Sublayers.Count; // sublayers

            return childCount;
        }

        public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
        {
            InspectView view = (InspectViewPeer)item;
            if (view == null)
                return (InspectViewPeer)Root.Peer;

            var subviewCount = (view.Subviews?.Count ?? 0);
            if (childIndex >= subviewCount) {
                if (childIndex == subviewCount && view.Layer != null)
                    return (InspectViewPeer)view.Layer.Peer;

                if (childIndex >= (view.Sublayers?.Count ?? 0))
                    return null;

                return (InspectViewPeer)view.Sublayers [(int)childIndex].Peer;
            }

            return (InspectViewPeer)view.Subviews [(int)childIndex].Peer;
        }

        public override NSObject GetObjectValue (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
        {
            InspectView view = (InspectViewPeer)item;
            if (view == null)
                return null;

            var text = (String.IsNullOrEmpty (view.DisplayName)) ? view.Type : view.DisplayName;
            if (text == null)
                return null;

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
                text += String.Format (" — “{0}”", view.Description);

            return new NSString (text);
        }
    }
}