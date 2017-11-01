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
using mshtml;

namespace Xamarin.CrossBrowser
{
	public partial class Node : EventTarget
	{
		internal Node (ScriptContext context, IHTMLDOMNode comObject) : base (context, (IEventTarget)comObject)
		{
		}

		public Node FirstChild {
			get {
				return Wrap<Node> (((IHTMLDOMNode)ComObject).firstChild);
			}
		}

		public Node LastChild {
			get {
				return Wrap<Node> (((IHTMLDOMNode)ComObject).lastChild);
			}
		}

		public Node NextSibling {
			get {
				return Wrap<Node> (((IHTMLDOMNode)ComObject).nextSibling);
			}
		}

		public string NodeName {
			get {
				return ((IHTMLDOMNode)ComObject).nodeName;
			}
		}

		public NodeType NodeType {
			get {
				return (NodeType)((IHTMLDOMNode)ComObject).nodeType;
			}
		}

		public string NodeValue {
			get {
				return Convert<string> (((IHTMLDOMNode)ComObject).nodeValue);
			}
			set {
				((IHTMLDOMNode)ComObject).nodeValue = value;
			}
		}

		public Document OwnerDocument {
			get {
				return Wrap<Document> (((IHTMLDOMNode2)ComObject).ownerDocument);
			}
		}

		public Node ParentNode {
			get {
				return Wrap<Node> (((IHTMLDOMNode)ComObject).parentNode);
			}
		}

		public Node PreviousSibling {
			get {
				return Wrap<Node> (((IHTMLDOMNode)ComObject).previousSibling);
			}
		}

		public string TextContent {
			get {
				return Convert<string> (((IHTMLDOMNode3)ComObject).textContent);
			}
			set {
				((IHTMLDOMNode3)ComObject).textContent = value;
			}
		}

		public Node AppendChild (Node child)
		{
			return Wrap<Node> (((IHTMLDOMNode)ComObject).appendChild ((IHTMLDOMNode)child.ComObject));
		}

		public Node CloneNode (bool deep)
		{
			return Wrap<Node> (((IHTMLDOMNode)ComObject).cloneNode (deep));
		}

		public DocumentPosition CompareDocumentPosition (Node other)
		{
			return (DocumentPosition)((IHTMLDOMNode3)ComObject).compareDocumentPosition ((IHTMLDOMNode)other.ComObject);
		}

		public bool HasChildNodes ()
		{
			return ((IHTMLDOMNode)ComObject).hasChildNodes ();
		}

		public Node InsertBefore (Node newNode, Node referenceNode)
		{
			return Wrap<Node> (((IHTMLDOMNode)ComObject).insertBefore ((IHTMLDOMNode)newNode.ComObject, (IHTMLDOMNode)referenceNode.ComObject));
		}

		public bool IsEqualNode (Node other)
		{
			return ((IHTMLDOMNode3)ComObject).isEqualNode ((IHTMLDOMNode3)other.ComObject);
		}

		public Node RemoveChild (Node child)
		{
			return Wrap<Node> (((IHTMLDOMNode)ComObject).removeChild ((IHTMLDOMNode)child.ComObject));
		}

		public Node ReplaceChild (Node newChild, Node oldChild)
		{
			return Wrap<Node> (((IHTMLDOMNode)ComObject).replaceChild ((IHTMLDOMNode)newChild.ComObject, (IHTMLDOMNode)oldChild.ComObject));
		}
	}
}