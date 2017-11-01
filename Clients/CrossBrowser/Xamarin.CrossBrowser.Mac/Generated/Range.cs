//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// Range.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public partial class Range : WrappedObject
    {
        internal Range (JSValue underlyingJSValue) : base (underlyingJSValue)
        {
        }

        public bool Collapsed {
            get {
                return UnderlyingJSValue.GetProperty ("collapsed").ToBool ();
            }
        }

        public Node CommonAncestorContainer {
            get {
                return Wrap<Node> (UnderlyingJSValue.GetProperty ("commonAncestorContainer"));
            }
        }

        public Node EndContainer {
            get {
                return Wrap<Node> (UnderlyingJSValue.GetProperty ("endContainer"));
            }
        }

        public int EndOffset {
            get {
                return UnderlyingJSValue.GetProperty ("endOffset").ToInt32 ();
            }
        }

        public Node StartContainer {
            get {
                return Wrap<Node> (UnderlyingJSValue.GetProperty ("startContainer"));
            }
        }

        public int StartOffset {
            get {
                return UnderlyingJSValue.GetProperty ("startOffset").ToInt32 ();
            }
        }

        public void SetStart (Node startNode, int startOffset)
        {
            UnderlyingJSValue.Invoke ("setStart", JSValue.From (startNode.UnderlyingJSValue, UnderlyingJSValue.Context), JSValue.From (startOffset, UnderlyingJSValue.Context));
        }

        public void SetEnd (Node endNode, int endOffset)
        {
            UnderlyingJSValue.Invoke ("setEnd", JSValue.From (endNode.UnderlyingJSValue, UnderlyingJSValue.Context), JSValue.From (endOffset, UnderlyingJSValue.Context));
        }

        public void SetStartBefore (Node referenceNode)
        {
            UnderlyingJSValue.Invoke ("setStartBefore", JSValue.From (referenceNode.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public void SetEndBefore (Node referenceNode)
        {
            UnderlyingJSValue.Invoke ("setEndBefore", JSValue.From (referenceNode.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public void SetStartAfter (Node referenceNode)
        {
            UnderlyingJSValue.Invoke ("setStartAfter", JSValue.From (referenceNode.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public void SetEndAfter (Node referenceNode)
        {
            UnderlyingJSValue.Invoke ("setEndAfter", JSValue.From (referenceNode.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public void Collapse (bool toStart = false)
        {
            UnderlyingJSValue.Invoke ("collapse", JSValue.From (toStart, UnderlyingJSValue.Context));
        }

        public void DeleteContents ()
        {
            UnderlyingJSValue.Invoke ("deleteContents");
        }

        public void SelectNode (Node referenceNode)
        {
            UnderlyingJSValue.Invoke ("selectNode", JSValue.From (referenceNode.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public void SelectNodeContents (Node referenceNode)
        {
            UnderlyingJSValue.Invoke ("selectNodeContents", JSValue.From (referenceNode.UnderlyingJSValue, UnderlyingJSValue.Context));
        }

        public ClientRect GetBoundingClientRect ()
        {
            return Wrap<ClientRect> (UnderlyingJSValue.Invoke ("getBoundingClientRect"));
        }
    }
}