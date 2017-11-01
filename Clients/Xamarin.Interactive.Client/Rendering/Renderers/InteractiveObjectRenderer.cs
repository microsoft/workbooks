//
// InteractiveObjectRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof (InteractiveObject), false)]
	sealed class InteractiveObjectRenderer : HtmlRendererBase
	{
		[Renderer (typeof (InteractiveObject.GetMemberValueError))]
		sealed class GetMemberValueErrorRenderer : HtmlRendererBase
		{
			public override string CssClass => "renderer-object-get-member-value-error";
			public override bool CanExpand => false;

			protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
			{
				yield return new RendererRepresentation ("Evaluation Error");
			}

			protected override void HandleRender (RenderTarget target)
			{
				var source = (InteractiveObject.GetMemberValueError)RenderState.Source;
				target.InlineTarget.InnerHTML = source.Exception == null
					? Catalog.GetString ("not evaluated")
					: Catalog.GetString ("cannot evaluate");
			}
		}

		public const string OPTION_SHOW_REF_MEMBER = "OPTION_SHOW_REF_MEMBER";

		const string DepthAttribute = "data-depth";

		static readonly RendererRepresentation reflectionRepresentation = new RendererRepresentation (
			"Object Members",
			options: RendererRepresentationOptions.ExpandedFromMenu,
			order: Int32.MaxValue - 1000); // almost last, but allow others to be more last... right?

		// FIXME: this is really particular to GetVars()...
		static readonly RendererRepresentation dictionaryRepresentation = new RendererRepresentation (
			"Dictionary Members", // this won't ever be shown with GetVars()
			options: RendererRepresentationOptions.ExpandedByDefault,
			order: Int32.MaxValue - 1000);

		public override string CssClass => "renderer-object";
		public override bool IsEnabled => true;
		public override bool CanExpand => IsEnabled &&
			((InteractiveObject)RenderState.Source).HasMembers &&
			!(RenderState.ParentState?.Source is InteractiveObject);

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			if (!(RenderState.ParentState?.Source is InteractiveObject)) {
				if (RenderState.Source is DictionaryInteractiveObject)
					yield return dictionaryRepresentation;
				else
					yield return reflectionRepresentation;
			}
		}

		RenderTarget renderTarget;
		bool hasPopulatedMembers;

		protected override async void HandleExpand ()
		{
			if (hasPopulatedMembers)
				return;

			var result = (InteractiveObject)await Context.InteractAsync (
				this,
				(InteractiveObject)RenderState.Source,
				new InteractiveObject.ReadAllMembersInteractMessage ());

			if (result == null)
				return;

			Bind (RenderState.WithSource (result));

			hasPopulatedMembers = true;

			// hack: see note in HandleRender below
			renderTarget.InlineTarget.RemoveChildren ();
			renderTarget.ExpandedTarget.RemoveChildren ();
			Render (renderTarget);
		}

		protected override void HandleRender (RenderTarget target)
		{
			// Storing the render target is a bit of a hack, but this works for now...
			// ideally RootRenderer would call Render() again after Expand()/Collapse()
			// with a freshly prepared target. For now we just store the original
			// target and clear its children out in HandleExpand. -abock, 2016-04-07
			renderTarget = target;

			var source = (InteractiveObject)RenderState.Source;
			var dictSource = source as DictionaryInteractiveObject;

			if (dictSource != null) {
				var headerElem = Document.CreateElement ("header");
				headerElem.AppendTextNode (dictSource.Title ?? String.Empty);
				target.InlineTarget.AppendChild (headerElem);
			} else if (!(RenderState.ParentState?.Source is InteractiveObject) && source.RepresentedType != null)
				target.InlineTarget.AppendChild (CreateHeaderElement (source.RepresentedType));

			if (source?.Members == null || source.Members.Length == 0)
				return;

			// the root object (depth=0) has been sent with its members,
			// so set this to avoid HandleExpand from fetching them again
			hasPopulatedMembers = true;

			var tableElem = Document.CreateElement ("table");

			var tbodyElem = Document.CreateElement ("tbody");
			tableElem.AppendChild (tbodyElem);

			var hasChildren = false;
			foreach (var row in RenderRows (source)) {
				hasChildren |= row.HasChildren;
				tbodyElem.AppendChild (row.Element);
			}

			if (hasChildren)
				tbodyElem.AddCssClass ("has-expandable-children");

			target.ExpandedTarget.AppendChild (tableElem);
		}

		struct Row
		{
			public HtmlElement Element;
			public bool HasChildren;
		}

		IEnumerable<Row> RenderRows (InteractiveObject obj)
		{
			for (var i = 0; i < obj.Members.Length; i++) {
				var row = RenderRow (obj, i);
				if (row.Element != null)
					yield return row;
			}
		}

		object UnpackMemberObject (InteractiveObject parent, int memberIndex,
			out InteractiveObject interactiveObject,
			out InteractiveObject.InteractMessage interactMessage)
		{
			interactMessage.MemberIndex = memberIndex;
			interactMessage.RepresentationIndex = -1;

			var value = parent.Values [memberIndex];
			interactiveObject = value as InteractiveObject;
			if (interactiveObject != null)
				return value;

			// FIXME?: InteractiveObjectRenderer will only support drilling down into
			// one InteractiveObject in a ResultCollection. In practive this should
			// be acceptable. Multiple InteractiveObject instances in a ResultCollection
			// is basically undefined behavior... we'll only support the first found.
			var resultCollection = value as RepresentedObject;
			for (var i = 0; resultCollection != null && i < resultCollection.Count; i++) {
				interactiveObject = resultCollection [i] as InteractiveObject;
				if (interactiveObject != null) {
					interactMessage.RepresentationIndex = i;
					return value;
				}
			}

			return value;
		}

		Row RenderRow (InteractiveObject obj, int memberIndex)
		{
			var member = obj.Members [memberIndex];
			if (member == null)
				return default (Row);

			InteractiveObject interactiveValue;
			InteractiveObject.InteractMessage interactMessage;
			var value = UnpackMemberObject (obj, memberIndex, out interactiveValue, out interactMessage);

			var hasChildren = interactiveValue != null && interactiveValue.HasMembers;

			var rowElem = Document.CreateElement ("tr");
			rowElem.SetAttribute (DepthAttribute, obj.Depth.ToString (NumberFormatInfo.InvariantInfo));

			var nameCellElem = Document.CreateElement ("th");
			nameCellElem.SetAttribute ("style", $"padding-left: {obj.Depth}em !important");
			rowElem.AppendChild (nameCellElem);

			var nameCellElemDiv = Document.CreateElement ("div", @class: "spaced-out-row");
			nameCellElem.AppendChild (nameCellElemDiv);

			var nameSpanElem = Document.CreateElement ("span");
			var expanderElem = Document.CreateElement ("div", @class: "xiui-expander-button");
			nameSpanElem.AppendChild (expanderElem);
			nameSpanElem.AppendTextNode (member.Name);
			nameCellElemDiv.AppendChild (nameSpanElem);

			object refNameOption;
			if (Context.Options.TryGetValue (OPTION_SHOW_REF_MEMBER, out refNameOption) && true.Equals (refNameOption)) {
				var refNameElem = Document.CreateElement ("div", @class: "inspect-ref-name");
				refNameElem.SetAttribute ("title", "Get a reference to this member");
				refNameElem.AddEventListener ("click", e => {
					Context.RaiseMemberReferenceRequested (new MemberReferenceRequestArgs (
						interactiveValue ?? obj,
						interactiveValue?.RepresentedType ?? member.DeclaringType,
						interactiveValue == null ? member.Name : null));
				});
				nameCellElemDiv.AppendChild (refNameElem);
			}

			var valueElem = Document.CreateElement ("td");
			rowElem.AppendChild (valueElem);

			var typeElem = Document.CreateElement ("td");
			typeElem.AppendChild (CreateRenderedTypeNameElement (member.MemberType));
			rowElem.AppendChild (typeElem);

			if (hasChildren) {
				rowElem.AddCssClass ("collapsed");

				nameSpanElem.AddEventListener ("click", async e => {
					if (WindowHasSelection ())
						return;
					var childElem = rowElem.NextElementSibling as HtmlElement;
					int childDepth;
					if (childElem == null ||
						!childElem.TryGetAttribute (DepthAttribute, out childDepth) ||
						childDepth != obj.Depth + 1) {
						expanderElem.AddCssClass ("expanded");
						await LoadValueAsync (rowElem, valueElem, obj, interactMessage);
					} else {
						RemoveValue (rowElem);
						expanderElem.RemoveCssClass ("expanded");
					}
				});
			} else
				expanderElem.AddCssClass ("spacer");

			Context.Render (RenderState.CreateChild (value,
				member.CanWrite
					? new RemoteMemberInfo {
						ObjectHandle = obj.RepresentedObjectHandle,
						MemberInfo = member
					}
					: null
			), valueElem);

			return new Row { Element = rowElem, HasChildren = hasChildren };
		}

		async Task LoadValueAsync (HtmlElement parentRowElem, HtmlElement valueElem,
			InteractiveObject obj, InteractiveObject.InteractMessage interactMessage)
		{
			var parent = parentRowElem.ParentElement;
			var insertBefore = parentRowElem.NextSibling;

			try {
				var result = (InteractiveObject)await Context.InteractAsync (
					this, obj, interactMessage);

				// Returns null when disconnected from agent
				if (result == null)
					return;

				parentRowElem.AddCssClass ("expanded");
				parentRowElem.RemoveCssClass ("collapsed");

				if (result.Members == null || result.Members.Length == 0) {
					parentRowElem.RemoveCssClass ("expanded");
					valueElem.RemoveChildren ();
					valueElem.AppendChild (Document.CreateElement ("code",
						innerHtml: result.ToStringRepresentation.HtmlEscape ()));
					return;
				}

				foreach (var row in RenderRows (result)) {
					// FIXME: JSC XCB InsertBefore binding throws an
					// NRE if insertBefore is null... it should instead
					// propagate the null to JS
					if (insertBefore != null)
						parent.InsertBefore (row.Element, insertBefore);
					else
						parent.AppendChild (row.Element);
				}
			} catch (Exception e) {
				valueElem.RemoveChildren ();
				Context.Render (RenderState.CreateChild (e), valueElem);
			}
		}

		static void RemoveValue (HtmlElement parentRowElem)
		{
			int parentDepth;
			if (!parentRowElem.TryGetAttribute (DepthAttribute, out parentDepth))
				return;

			parentRowElem.RemoveCssClass ("expanded");
			parentRowElem.AddCssClass ("collapsed");

			var elem = parentRowElem.NextElementSibling;
			while (elem != null) {
				int depth;
				if (!((HtmlElement)elem).TryGetAttribute (DepthAttribute, out depth) ||
					depth <= parentDepth)
					return;

				var nextElem = elem.NextElementSibling;
				elem.ParentElement.RemoveChild (elem);
				elem = nextElem;
			}
		}
	}
}