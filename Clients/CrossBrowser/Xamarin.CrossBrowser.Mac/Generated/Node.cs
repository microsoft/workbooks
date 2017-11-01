//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// Node.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class Node : EventTarget
	{
		internal Node (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public Node FirstChild {
			get {
				return Wrap<Node> (UnderlyingJSValue.GetProperty ("firstChild"));
			}
		}

		public Node LastChild {
			get {
				return Wrap<Node> (UnderlyingJSValue.GetProperty ("lastChild"));
			}
		}

		public Node NextSibling {
			get {
				return Wrap<Node> (UnderlyingJSValue.GetProperty ("nextSibling"));
			}
		}

		public string NodeName {
			get {
				return UnderlyingJSValue.GetProperty ("nodeName").ToNullableString ();
			}
		}

		public NodeType NodeType {
			get {
				return (NodeType)UnderlyingJSValue.GetProperty ("nodeType").ToUInt32 ();
			}
		}

		public string NodeValue {
			get {
				return UnderlyingJSValue.GetProperty ("nodeValue").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "nodeValue");
			}
		}

		public Document OwnerDocument {
			get {
				return Wrap<Document> (UnderlyingJSValue.GetProperty ("ownerDocument"));
			}
		}

		public Node ParentNode {
			get {
				return Wrap<Node> (UnderlyingJSValue.GetProperty ("parentNode"));
			}
		}

		public Node PreviousSibling {
			get {
				return Wrap<Node> (UnderlyingJSValue.GetProperty ("previousSibling"));
			}
		}

		public string TextContent {
			get {
				return UnderlyingJSValue.GetProperty ("textContent").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "textContent");
			}
		}

		public Node AppendChild (Node child)
		{
			return Wrap<Node> (UnderlyingJSValue.Invoke ("appendChild", JSValue.From (child.UnderlyingJSValue, UnderlyingJSValue.Context)));
		}

		public Node CloneNode (bool deep)
		{
			return Wrap<Node> (UnderlyingJSValue.Invoke ("cloneNode", JSValue.From (deep, UnderlyingJSValue.Context)));
		}

		public DocumentPosition CompareDocumentPosition (Node other)
		{
			return (DocumentPosition)UnderlyingJSValue.Invoke ("compareDocumentPosition", JSValue.From (other.UnderlyingJSValue, UnderlyingJSValue.Context)).ToUInt32 ();
		}

		public bool HasChildNodes ()
		{
			return UnderlyingJSValue.Invoke ("hasChildNodes").ToBool ();
		}

		public Node InsertBefore (Node newNode, Node referenceNode)
		{
			return Wrap<Node> (UnderlyingJSValue.Invoke ("insertBefore", JSValue.From (newNode.UnderlyingJSValue, UnderlyingJSValue.Context), JSValue.From (referenceNode.UnderlyingJSValue, UnderlyingJSValue.Context)));
		}

		public bool IsEqualNode (Node other)
		{
			return UnderlyingJSValue.Invoke ("isEqualNode", JSValue.From (other.UnderlyingJSValue, UnderlyingJSValue.Context)).ToBool ();
		}

		public Node RemoveChild (Node child)
		{
			return Wrap<Node> (UnderlyingJSValue.Invoke ("removeChild", JSValue.From (child.UnderlyingJSValue, UnderlyingJSValue.Context)));
		}

		public Node ReplaceChild (Node newChild, Node oldChild)
		{
			return Wrap<Node> (UnderlyingJSValue.Invoke ("replaceChild", JSValue.From (newChild.UnderlyingJSValue, UnderlyingJSValue.Context), JSValue.From (oldChild.UnderlyingJSValue, UnderlyingJSValue.Context)));
		}
	}
}