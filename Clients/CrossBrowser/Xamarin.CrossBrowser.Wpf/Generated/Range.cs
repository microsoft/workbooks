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
using mshtml;

namespace Xamarin.CrossBrowser
{
	public partial class Range : WrappedObject
	{
		internal Range (ScriptContext context, IHTMLDOMRange comObject) : base (context, comObject)
		{
		}

		public bool Collapsed {
			get {
				return ((IHTMLDOMRange)ComObject).collapsed;
			}
		}

		public Node CommonAncestorContainer {
			get {
				return Wrap<Node> (((IHTMLDOMRange)ComObject).commonAncestorContainer);
			}
		}

		public Node EndContainer {
			get {
				return Wrap<Node> (((IHTMLDOMRange)ComObject).endContainer);
			}
		}

		public int EndOffset {
			get {
				return ((IHTMLDOMRange)ComObject).endOffset;
			}
		}

		public Node StartContainer {
			get {
				return Wrap<Node> (((IHTMLDOMRange)ComObject).startContainer);
			}
		}

		public int StartOffset {
			get {
				return ((IHTMLDOMRange)ComObject).startOffset;
			}
		}

		public void SetStart (Node startNode, int startOffset)
		{
			((IHTMLDOMRange)ComObject).setStart ((IHTMLDOMNode)startNode.ComObject, startOffset);
		}

		public void SetEnd (Node endNode, int endOffset)
		{
			((IHTMLDOMRange)ComObject).setEnd ((IHTMLDOMNode)endNode.ComObject, endOffset);
		}

		public void SetStartBefore (Node referenceNode)
		{
			((IHTMLDOMRange)ComObject).setStartBefore ((IHTMLDOMNode)referenceNode.ComObject);
		}

		public void SetEndBefore (Node referenceNode)
		{
			((IHTMLDOMRange)ComObject).setEndBefore ((IHTMLDOMNode)referenceNode.ComObject);
		}

		public void SetStartAfter (Node referenceNode)
		{
			((IHTMLDOMRange)ComObject).setStartAfter ((IHTMLDOMNode)referenceNode.ComObject);
		}

		public void SetEndAfter (Node referenceNode)
		{
			((IHTMLDOMRange)ComObject).setEndAfter ((IHTMLDOMNode)referenceNode.ComObject);
		}

		public void Collapse (bool toStart = false)
		{
			((IHTMLDOMRange)ComObject).collapse (toStart);
		}

		public void DeleteContents ()
		{
			((IHTMLDOMRange)ComObject).deleteContents ();
		}

		public void SelectNode (Node referenceNode)
		{
			((IHTMLDOMRange)ComObject).selectNode ((IHTMLDOMNode)referenceNode.ComObject);
		}

		public void SelectNodeContents (Node referenceNode)
		{
			((IHTMLDOMRange)ComObject).selectNodeContents ((IHTMLDOMNode)referenceNode.ComObject);
		}

		public ClientRect GetBoundingClientRect ()
		{
			return Wrap<ClientRect> (((IHTMLDOMRange)ComObject).getBoundingClientRect ());
		}
	}
}