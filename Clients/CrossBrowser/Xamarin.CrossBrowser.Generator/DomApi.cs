//
// DomApi.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// 
// Analysis disable CheckNamespace

using System;

[Flags]
enum DocumentPosition : ushort
{
	None = 0,
	Disconnected = 1,
	Preceding = 2,
	Following = 4,
	Contains = 8,
	ContainedBy = 16,
	ImplementationSpecific = 32
}

enum NodeType : ushort
{
	None = 0,
	Element = 1,
	[Obsolete] Attribute = 2,
	Text = 3,
	[Obsolete] CDataSection = 4,
	[Obsolete] EntityReference = 5,
	[Obsolete] Entity = 6,
	ProcessingInstruction = 7,
	Comment = 8,
	Document = 9,
	DocumentType = 10,
	DocumentFragment = 11,
	[Obsolete] Notation = 12
}

enum EventPhase : uint
{
	None = 0,
	Capturing = 1,
	AtTarget = 2,
	Bubbling = 3
}

[Type (Backend.Mshtml, "Object")]
interface WrappedObject
{
	[Ignore (Backend.Mshtml)] string ToString ();
}

[Type (Backend.Mshtml, "IDOMEvent")]
interface Event : WrappedObject
{
	bool Bubbles { get; }
	bool Cancelable { get; }
	EventTarget CurrentTarget { get; }
	bool DefaultPrevented { get; }
	EventPhase EventPhase { get; }
	EventTarget Target { get; }
	string Type { get; }
	bool IsTrusted { get; }

	void PreventDefault ();
	void StopImmediatePropagation ();
	void StopPropagation ();
}

[Type (Backend.Mshtml, "IDOMUIEvent", "IDOMEvent")]
interface UIEvent : Event
{
}

[Type (Backend.Mshtml, "IDOMKeyboardEvent", "IDOMUIEvent")]
interface KeyboardEvent : UIEvent
{
	bool AltKey { get; }
	bool CtrlKey { get; }
	bool MetaKey { get; }
	bool ShiftKey { get; }
	bool Repeat { get; }
	int KeyCode { get; }
	int CharCode { get; }
	string Key { get; }
}

interface EventListener
{
}

[Type (Backend.Mshtml, "IEventTarget", "Object")]
interface EventTarget : WrappedObject
{
	void AddEventListener (string type, EventListener listener, bool useCapture = false);
	void RemoveEventListener (string type, EventListener listener, bool useCapture = false);
	bool DispatchEvent (Event @event);
}

[Type (Backend.Mshtml, "IHTMLRect", "Object")]
interface ClientRect : WrappedObject
{
	double Left { get; }
	double Top { get; }
	double Right { get; }
	double Bottom { get; }
	[Type (Backend.Mshtml, "IHTMLRect2")] double Width { get; }
	[Type (Backend.Mshtml, "IHTMLRect2")] double Height { get; }
}

[Type (Backend.Mshtml, "IHTMLDOMNode", "IEventTarget")]
interface Node : EventTarget
{
	Node FirstChild { get; }
	Node LastChild { get; }
	Node NextSibling { get; }
	string NodeName { get; }
	NodeType NodeType { get; }
	[MshtmlConvert] string NodeValue { get; set; }
	[Type (Backend.Mshtml, "IHTMLDOMNode2")] Document OwnerDocument { get; }
	Node ParentNode { get; }
	Node PreviousSibling { get; }
	[Type (Backend.Mshtml, "IHTMLDOMNode3"), MshtmlConvert] string TextContent { get; set; }
	Node AppendChild (Node child);
	Node CloneNode (bool deep);
	[Type (Backend.Mshtml, "IHTMLDOMNode3")] DocumentPosition CompareDocumentPosition (Node other);
	bool HasChildNodes ();
	Node InsertBefore (Node newNode, Node referenceNode);
	[Type (Backend.Mshtml, "IHTMLDOMNode3")] bool IsEqualNode ([Type (Backend.Mshtml, "IHTMLDOMNode3")] Node other);
	Node RemoveChild (Node child);
	Node ReplaceChild (Node newChild, Node oldChild);
}

[Type (Backend.Mshtml, "IHTMLDOMTextNode", "IHTMLDOMNode")]
interface Text : Node
{
}

[Type (Backend.Mshtml, "IHTMLDOMRange")]
interface Range : WrappedObject
{
	bool Collapsed { get; }
	Node CommonAncestorContainer { get; }
	Node EndContainer { get; }
	int EndOffset { get; }
	Node StartContainer { get; }
	int StartOffset { get; }

	void SetStart (Node startNode, int startOffset);
	void SetEnd (Node endNode, int endOffset);
	void SetStartBefore (Node referenceNode);
	void SetEndBefore (Node referenceNode);
	void SetStartAfter (Node referenceNode);
	void SetEndAfter (Node referenceNode);

	void Collapse (bool toStart = false);
	void DeleteContents ();
	void SelectNode (Node referenceNode);
	void SelectNodeContents (Node referenceNode);

	ClientRect GetBoundingClientRect ();
}

[Type (Backend.Mshtml, "IHTMLSelection")]
interface Selection : WrappedObject
{
	Node AnchorNode { get; }
	int AnchorOffset { get; }
	Node FocusNode { get; }
	int FocusOffset { get; }
	bool IsCollapsed { get; }
	int RangeCount { get; }

	Range GetRangeAt (int index);
	void Collapse (Node parentNode, int offset);
	void CollapseToStart ();
	void CollapseToEnd ();
	void SelectAllChildren (Node parentNode);
	void AddRange (Range range);
	void RemoveRange (Range range);
	void RemoveAllRanges ();
	void DeleteFromDocument ();
}

interface ScrollIntoViewOptions
{
}

[Type (Backend.Mshtml, "IHTMLElement", "IHTMLDOMNode")]
interface Element : Node
{
	string TagName { get; }
	string ClassName { get; set; }
	string Id { get; set; }
	string InnerHTML { get; set; }
	string OuterHTML { get; set; }
	Element ParentElement { get; }

	[Type (Backend.Mshtml, "IHTMLElement2")] double ClientTop { get; }
	[Type (Backend.Mshtml, "IHTMLElement2")] double ClientLeft { get; }
	[Type (Backend.Mshtml, "IHTMLElement2")] double ClientHeight { get; }
	[Type (Backend.Mshtml, "IHTMLElement2")] double ClientWidth { get; }


	[Type (Backend.Mshtml, "IElementTraversal")] int ChildElementCount { get; }
	[Type (Backend.Mshtml, "IElementTraversal")] Element FirstElementChild { get; }
	[Type (Backend.Mshtml, "IElementTraversal")] Element LastElementChild { get; }
	[Type (Backend.Mshtml, "IElementTraversal")] Element NextElementSibling { get; }
	[Type (Backend.Mshtml, "IElementTraversal")] Element PreviousElementSibling { get; }

	bool Contains (Element other);

	[Type (Backend.Mshtml, "IHTMLElement4")] void Normalize ();

	[Type (Backend.Mshtml, "IHTMLElement5")] void SetAttribute (string name, string value);
	[Type (Backend.Mshtml, "IHTMLElement5")] bool HasAttribute (string name);
	[Type (Backend.Mshtml, "IHTMLElement5"), MshtmlConvert] string GetAttribute (string name);
	[Type (Backend.Mshtml, "IHTMLElement5")] void RemoveAttribute (string name);

	[Type (Backend.Mshtml, "IHTMLElement2")] ClientRect GetBoundingClientRect ();

	void ScrollIntoView (bool alignToTop = true);
	[Ignore (Backend.Mshtml)] void ScrollIntoView (ScrollIntoViewOptions options);
}

[Type (Backend.Mshtml, "IHTMLElement", "IHTMLElement")]
interface HtmlElement : Element
{
	[Type (Backend.Mshtml, "IHTMLElement3")] string ContentEditable { get; set; }
	[Type (Backend.Mshtml, "IHTMLElement3")] bool IsContentEditable { get; }

	CssStyleDeclaration Style { get; }

	[Type (Backend.Mshtml, "IHTMLElement2")] void Focus ();
	[Type (Backend.Mshtml, "IHTMLElement2")] void Blur ();
	void Click ();

	[Type (Backend.Mshtml, "IHTMLElement2")] int ScrollTop { get; set; }
	[Type (Backend.Mshtml, "IHTMLElement2")] int ScrollLeft { get; set; }
	[Type (Backend.Mshtml, "IHTMLElement2")] int ScrollWidth{ get; }
	[Type (Backend.Mshtml, "IHTMLElement2")] int ScrollHeight { get; }

	double OffsetTop { get; }
	double OffsetLeft { get; }
	double OffsetHeight { get; }
	double OffsetWidth { get; }

	HtmlElement OffsetParent { get; }
}

[Type (Backend.Mshtml, "IHTMLInputElement", "IHTMLElement")]
interface HtmlInputElement : HtmlElement
{
	string Type { get; set; }
	string Value { get; set; }
}

[Type (Backend.Mshtml, "IHTMLStyleElement", "IHTMLElement")]
interface HtmlStyleElement : HtmlElement
{
	[Type (Backend.Mshtml, "IHTMLStyleElement2")] CssStyleSheet Sheet { get; }
	string Type { get; set; }
	string Media { get; set; }
	bool Disabled { get; set; }
}

[Type (Backend.Mshtml, "IHTMLDocument2", "IHTMLDOMNode")]
interface Document : Node
{
	[Type (Backend.Mshtml, "IHTMLDocument3")] Element DocumentElement { get; }

	[Type (Backend.Mshtml, "IHTMLDocument7")] Selection GetSelection ();
	Element CreateElement (string name);
	[Type (Backend.Mshtml, "IHTMLDocument3")] Node CreateTextNode (string text);
	[Type (Backend.Mshtml, "IDocumentRange")] Range CreateRange ();
	[Type (Backend.Mshtml, "IHTMLDocument3")] Element GetElementById (string id);

	StyleSheetList StyleSheets { get; }
}

[Type (Backend.Mshtml, "IHTMLDocument2", "IHTMLDocument2")]
interface HtmlDocument : Document
{
	[Type (Backend.Mshtml, "IHTMLDocument7")] HtmlElement Head { get; }
	HtmlElement Body { get; }
	[Type (Backend.Mshtml, "IHTMLDocument3")]
	[Ignore (Backend.JavaScriptCore)]
	new HtmlElement DocumentElement { get; }
	new HtmlElement CreateElement (string name);
	[Type (Backend.Mshtml, "IHTMLDocument3")] new HtmlElement GetElementById (string id);
}

[Type (Backend.Mshtml, "IHTMLStyleSheetsCollection", "Object")]
[IReadOnlyList]
interface StyleSheetList : WrappedObject
{
	int Length { get; }
	[Type (Backend.Mshtml, "IHTMLStyleSheetsCollection2")] CssStyleSheet Item (int index);
}

[Type (Backend.Mshtml, "IHTMLStyleSheet", "Object")]
interface StyleSheet : WrappedObject
{
	string Href { get; }
	string Id { get; }
	[Type (Backend.Mshtml, "IHTMLStyleSheet4")] string Title { get; }
	[Type (Backend.Mshtml, "IHTMLStyleSheet4")] string Type { get; }
	[Type (Backend.Mshtml, "IHTMLStyleSheet4")] Node OwnerNode { get; }
}

[Type (Backend.Mshtml, "IHTMLStyleSheet", "IHTMLStyleSheet")]
interface CssStyleSheet : StyleSheet
{
	[Type (Backend.Mshtml, "IHTMLStyleSheet4")] void InsertRule (string rule, int index);
	[Type (Backend.Mshtml, "IHTMLStyleSheet4")] void DeleteRule (int index);
}

enum CssRuleType : ushort
{
	None,
	Style = 1,
	[Obsolete] Charset = 2,
	Import = 3,
	Media = 4,
	FontFace = 5,
	PAGE_RULE = 6,
	KeyFrames = 7,
	KeyFrame = 8,
	Namespace = 10,
	CounterStyle = 11,
	Supports = 12,
	Document = 13,
	FontFeaturesValue = 14,
	Viewport = 15,
	RegionStyle = 16,
}

[Type (Backend.Mshtml, "IHTMLCSSRule", "Object")]
interface CssRule : WrappedObject
{
	string CssText { get; }
	CssRule ParentRule { get; }
	CssStyleSheet ParentStyleSheet { get; }
	CssRuleType Type { get; }
}

[Type (Backend.Mshtml, "IHTMLCSSStyleDeclaration", "Object")]
interface CssStyleDeclaration : WrappedObject
{
	string CssText { get; set; }
	int Length { get; }
	CssRule ParentRule { get; }

	string Item (int index);
	void SetProperty (string name, string value, string priority = "");
	void RemoveProperty (string name);
	string GetPropertyValue (string name);
	string GetPropertyPriority (string name);
}