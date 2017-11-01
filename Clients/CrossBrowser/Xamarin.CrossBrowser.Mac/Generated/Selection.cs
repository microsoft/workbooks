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
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public partial class Selection : WrappedObject
    {
        internal Selection (JSValue underlyingJSValue) : base (underlyingJSValue)
        {
        }

        public Node AnchorNode {
            get {
                return Wrap<Node> (UnderlyingJSValue.GetProperty ("anchorNode"));
            }
        }

        public int AnchorOffset {
            get {
                return UnderlyingJSValue.GetProperty ("anchorOffset").ToInt32 ();
            }
        }

        public Node FocusNode {
            get {
                return Wrap<Node> (UnderlyingJSValue.GetProperty ("focusNode"));
            }
        }

        public int FocusOffset {
            get {
                return UnderlyingJSValue.GetProperty ("focusOffset").ToInt32 ();
            }
        }

        public bool IsCollapsed {
            get {
                return UnderlyingJSValue.GetProperty ("isCollapsed").ToBool ();
            }
        }

        public int RangeCount {
            get {
                return UnderlyingJSValue.GetProperty ("rangeCount").ToInt32 ();
            }
        }

        public Range GetRangeAt (int index)
        {
            return Wrap<Range> (UnderlyingJSValue.Invoke ("getRangeAt", JSValue.From (index, UnderlyingJSValue.Context)));
        }

        public void Collapse (Node parentNode, int offset)
        {
            UnderlyingJSValue.Invoke ("collapse", JSValue.From (parentNode.UnderlyingJSValue, UnderlyingJSValue.Context), JSValue.From (offset, UnderlyingJSValue.Context));
        }

        public void CollapseToStart ()
        {
            UnderlyingJSValue.Invoke ("collapseToStart");
        }

        public void CollapseToEnd ()
        {
            UnderlyingJSValue.Invoke ("collapseToEnd");
        }

        public void SelectAllChildren (Node parentNode)
        {
            UnderlyingJSValue.Invoke ("selectAllChildren", JSValue.From (parentNode.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public void AddRange (Range range)
        {
            UnderlyingJSValue.Invoke ("addRange", JSValue.From (range.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public void RemoveRange (Range range)
        {
            UnderlyingJSValue.Invoke ("removeRange", JSValue.From (range.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public void RemoveAllRanges ()
        {
            UnderlyingJSValue.Invoke ("removeAllRanges");
        }

        public void DeleteFromDocument ()
        {
            UnderlyingJSValue.Invoke ("deleteFromDocument");
        }
    }
}