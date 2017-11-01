//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// Element.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	public partial class Element : Node
	{
		internal Element (JSValue underlyingJSValue) : base (underlyingJSValue)
		{
		}

		public string TagName {
			get {
				return UnderlyingJSValue.GetProperty ("tagName").ToNullableString ();
			}
		}

		public string ClassName {
			get {
				return UnderlyingJSValue.GetProperty ("className").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "className");
			}
		}

		public string Id {
			get {
				return UnderlyingJSValue.GetProperty ("id").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "id");
			}
		}

		public string InnerHTML {
			get {
				return UnderlyingJSValue.GetProperty ("innerHTML").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "innerHTML");
			}
		}

		public string OuterHTML {
			get {
				return UnderlyingJSValue.GetProperty ("outerHTML").ToNullableString ();
			}
			set {
				UnderlyingJSValue.SetProperty (JSValue.From (value, UnderlyingJSValue.Context), "outerHTML");
			}
		}

		public Element ParentElement {
			get {
				return Wrap<Element> (UnderlyingJSValue.GetProperty ("parentElement"));
			}
		}

		public double ClientTop {
			get {
				return UnderlyingJSValue.GetProperty ("clientTop").ToDouble ();
			}
		}

		public double ClientLeft {
			get {
				return UnderlyingJSValue.GetProperty ("clientLeft").ToDouble ();
			}
		}

		public double ClientHeight {
			get {
				return UnderlyingJSValue.GetProperty ("clientHeight").ToDouble ();
			}
		}

		public double ClientWidth {
			get {
				return UnderlyingJSValue.GetProperty ("clientWidth").ToDouble ();
			}
		}

		public int ChildElementCount {
			get {
				return UnderlyingJSValue.GetProperty ("childElementCount").ToInt32 ();
			}
		}

		public Element FirstElementChild {
			get {
				return Wrap<Element> (UnderlyingJSValue.GetProperty ("firstElementChild"));
			}
		}

		public Element LastElementChild {
			get {
				return Wrap<Element> (UnderlyingJSValue.GetProperty ("lastElementChild"));
			}
		}

		public Element NextElementSibling {
			get {
				return Wrap<Element> (UnderlyingJSValue.GetProperty ("nextElementSibling"));
			}
		}

		public Element PreviousElementSibling {
			get {
				return Wrap<Element> (UnderlyingJSValue.GetProperty ("previousElementSibling"));
			}
		}

		public bool Contains (Element other)
		{
			return UnderlyingJSValue.Invoke ("contains", JSValue.From (other.UnderlyingJSValue, UnderlyingJSValue.Context)).ToBool ();
		}

		public void Normalize ()
		{
			UnderlyingJSValue.Invoke ("normalize");
		}

		public void SetAttribute (string name, string value)
		{
			UnderlyingJSValue.Invoke ("setAttribute", JSValue.From (name, UnderlyingJSValue.Context), JSValue.From (value, UnderlyingJSValue.Context));
		}

		public bool HasAttribute (string name)
		{
			return UnderlyingJSValue.Invoke ("hasAttribute", JSValue.From (name, UnderlyingJSValue.Context)).ToBool ();
		}

		public string GetAttribute (string name)
		{
			return UnderlyingJSValue.Invoke ("getAttribute", JSValue.From (name, UnderlyingJSValue.Context)).ToNullableString ();
		}

		public void RemoveAttribute (string name)
		{
			UnderlyingJSValue.Invoke ("removeAttribute", JSValue.From (name, UnderlyingJSValue.Context));
		}

		public ClientRect GetBoundingClientRect ()
		{
			return Wrap<ClientRect> (UnderlyingJSValue.Invoke ("getBoundingClientRect"));
		}

		public void ScrollIntoView (bool alignToTop = true)
		{
			UnderlyingJSValue.Invoke ("scrollIntoView", JSValue.From (alignToTop, UnderlyingJSValue.Context));
		}

		public void ScrollIntoView (ScrollIntoViewOptions options)
		{
			UnderlyingJSValue.Invoke ("scrollIntoView", JSValue.From (options, UnderlyingJSValue.Context));
		}
	}
}