//
// DomExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Xamarin.CrossBrowser;

namespace Xamarin.Interactive.Rendering
{
	static class DomExtensions
	{
		public static HtmlElement CreateElement (
			this Document document,
			string name,
			string @class = null,
			string style = null,
			string innerHtml = null,
			string innerText = null)
		{
			if (innerHtml != null && innerText != null)
				throw new ArgumentException ("cannot specify both innerHtml and innerText");

			var elem = (HtmlElement)document.CreateElement (name);
			if (@class != null)
				elem.SetAttribute ("class", @class);
			if (style != null)
				elem.SetAttribute ("style", style);
			if (innerHtml != null)
				elem.InnerHTML = innerHtml;
			if (innerText != null)
				elem.AppendTextNode (innerText);
			return elem;
		}

		static readonly Regex whitespaceRegex = new Regex (@"\s+");

		public static List<string> GetCssClasses (this HtmlElement element)
		{
			var classes = element.ClassName;
			if (classes == null)
				return new List<string> ();
			return new List<string> (whitespaceRegex.Split (classes));
		}

		public static void SetCssClasses (this HtmlElement element, IEnumerable<string> classNames)
		{
			element.ClassName = String.Join (" ", classNames);
		}

		public static bool HasCssClass (this HtmlElement element, string className)
		{
			return className != null && GetCssClasses (element).Contains (className);
		}

		public static bool RemoveCssClass (this HtmlElement element, string className)
		{
			if (className == null)
				return false;

			var classes = GetCssClasses (element);
			if (classes.Remove (className)) {
				SetCssClasses (element, classes);
				return true;
			}

			return false;
		}

		public static void AddCssClass (this HtmlElement element, string className)
		{
			if (className == null)
				return;

			var classes = GetCssClasses (element);
			if (!classes.Contains (className)) {
				classes.Add (className);
				SetCssClasses (element, classes);
			}
		}

		public static void ToggleCssClass (this HtmlElement element, string className)
		{
			if (className == null)
				return;

			if (!element.RemoveCssClass (className))
				element.AddCssClass (className);
		}

		public static void RemoveChildren (this HtmlElement element)
		{
			while (true) {
				var child = element.FirstChild;
				if (child == null)
					break;
				element.RemoveChild (child);
			}
		}

		public static bool TryGetAttribute (this HtmlElement element, string name, out int value)
		{
			value = 0;
			var strValue = element?.GetAttribute (name);
			if (strValue == null)
				return false;
			return Int32.TryParse (strValue, out value);
		}

		public static void AppendTextNode (this Node node, string textContent)
			=> node.AppendChild (node.OwnerDocument.CreateTextNode (textContent));
	}
}