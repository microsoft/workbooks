//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// Selection.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class Selection : WrappedObject
    {
        internal Selection (ScriptContext context, IHTMLSelection comObject) : base (context, comObject)
        {
        }

        public Node AnchorNode {
            get {
                return Wrap<Node> (((IHTMLSelection)ComObject).anchorNode);
            }
        }

        public int AnchorOffset {
            get {
                return ((IHTMLSelection)ComObject).anchorOffset;
            }
        }

        public Node FocusNode {
            get {
                return Wrap<Node> (((IHTMLSelection)ComObject).focusNode);
            }
        }

        public int FocusOffset {
            get {
                return ((IHTMLSelection)ComObject).focusOffset;
            }
        }

        public bool IsCollapsed {
            get {
                return ((IHTMLSelection)ComObject).isCollapsed;
            }
        }

        public int RangeCount {
            get {
                return ((IHTMLSelection)ComObject).rangeCount;
            }
        }

        public Range GetRangeAt (int index)
        {
            return Wrap<Range> (((IHTMLSelection)ComObject).getRangeAt (index));
        }

        public void Collapse (Node parentNode, int offset)
        {
            ((IHTMLSelection)ComObject).collapse ((IHTMLDOMNode)parentNode.ComObject, offset);
        }

        public void CollapseToStart ()
        {
            ((IHTMLSelection)ComObject).collapseToStart ();
        }

        public void CollapseToEnd ()
        {
            ((IHTMLSelection)ComObject).collapseToEnd ();
        }

        public void SelectAllChildren (Node parentNode)
        {
            ((IHTMLSelection)ComObject).selectAllChildren ((IHTMLDOMNode)parentNode.ComObject);
        }

        public void AddRange (Range range)
        {
            ((IHTMLSelection)ComObject).addRange ((IHTMLDOMRange)range.ComObject);
        }

        public void RemoveRange (Range range)
        {
            ((IHTMLSelection)ComObject).removeRange ((IHTMLDOMRange)range.ComObject);
        }

        public void RemoveAllRanges ()
        {
            ((IHTMLSelection)ComObject).removeAllRanges ();
        }

        public void DeleteFromDocument ()
        {
            ((IHTMLSelection)ComObject).deleteFromDocument ();
        }
    }
}