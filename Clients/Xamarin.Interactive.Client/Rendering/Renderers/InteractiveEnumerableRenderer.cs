//
// InteractiveEnumerableRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof (InteractiveEnumerable))]
	sealed class InteractiveEnumerableRenderer : HtmlRendererBase
	{
		public override string CssClass => "renderer-enumerable";

		HtmlElement headerElem;
		HtmlElement footerElem;
		HtmlElement loadMoreItemsElem;
		HtmlElement itemsElem;

		InteractiveEnumerable source;

		protected override void HandleBind ()
		{
			source = (InteractiveEnumerable)RenderState.Source;

			if (source.IsLastSlice && footerElem?.ParentElement != null) {
				footerElem.ParentElement.RemoveChild (footerElem);
				footerElem = null;
			}
		}

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return new RendererRepresentation (
				"Enumerable", options: RendererRepresentationOptions.ForceExpand);
		}

		protected override void HandleRender (RenderTarget target)
		{
			target.InlineTarget.AppendChild (RenderHeader ());

			if (target.IsExpanded) {
				itemsElem = Document.CreateElement ("ol");
				target.ExpandedTarget.AppendChild (itemsElem);

				RenderSlice ();
				RenderFooter ();

				if (footerElem != null)
					target.ExpandedTarget.AppendChild (footerElem);

				if (source.Depth == 0)
					ExpandOrCollapse (expand: true);
			}
		}

		HtmlElement RenderHeader ()
		{
			if (headerElem == null) {
				headerElem = CreateHeaderElement (source.RepresentedType,
					source.Count > 0 ? String.Format ("{0} items", source.Count) : null);
				headerElem.AddEventListener ("click", async e => {
					if (WindowHasSelection ())
						return;
					await ToggleExpandedWithLoadAsync ();
				});
			}

			return headerElem;
		}

		HtmlElement RenderFooter ()
		{
			if (footerElem == null && !source.IsLastSlice) {
				loadMoreItemsElem = Document.CreateElement ("div", @class: "xiui-button");
				loadMoreItemsElem.InnerHTML = "Continue Enumerating&hellip;";
				loadMoreItemsElem.AddEventListener ("click", async e => {
					if (WindowHasSelection ())
						return;
					await LoadItemsAsync ();
				});

				footerElem = Document.CreateElement ("footer");
				footerElem.AppendChild (loadMoreItemsElem);
			}

			return footerElem;
		}

		void RenderSlice ()
		{
			var lastChildIsEnumerable = false;

			if (source.Slice != null) {
				foreach (var item in source.Slice) {
					var itemElem = Document.CreateElement ("li");
					itemsElem.AppendChild (itemElem);
					RenderState.Context.Render (RenderState.CreateChild (item), itemElem);
					lastChildIsEnumerable = item is InteractiveEnumerable;
				}
			}

			if (lastChildIsEnumerable)
				itemsElem.RemoveCssClass ("intermediate");
			else
				itemsElem.AddCssClass ("intermediate");
		}

		async Task ToggleExpandedWithLoadAsync ()
		{
			var expanded = headerElem.HasCssClass ("expanded");
			if (!expanded && source.Slice == null)
				await LoadItemsAsync ();
			ExpandOrCollapse (!expanded);
		}

		void ExpandOrCollapse (bool expand)
		{
			foreach (var elem in new [] { headerElem, itemsElem, footerElem }) {
				if (expand)
					elem?.AddCssClass ("expanded");
				else
					elem?.RemoveCssClass ("expanded");
			}
		}

		async Task LoadItemsAsync ()
		{
			var obj = await Context.InteractAsync (this, source);
			if (obj == null)
				return;

			Bind (RenderState.WithSource (obj));
			RenderSlice ();
			footerElem?.ScrollIntoView ();
		}
	}
}