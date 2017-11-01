//
// JavaScriptRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using Xamarin.CrossBrowser;

namespace Xamarin.Interactive.Rendering
{
	// NOTE: no support for RenderState.ParentState since RenderState
	// does not have a JS peer. It doesn't really make sense anyway
	// when crossing the .NET <-> JS boundary. JS renderers that may
	// create children can have access to their JS parent states however.
	sealed class JavaScriptRenderer : IRenderer
	{
		#pragma warning disable 0414
		readonly ScriptContext scriptContext;
		readonly dynamic jsPeer;
		#pragma warning restore 0414

		public JavaScriptRenderer (ScriptContext scriptContext, dynamic jsPeer)
		{
			if (scriptContext == null)
				throw new ArgumentNullException (nameof (scriptContext));

			if (jsPeer == null)
				throw new ArgumentNullException (nameof (jsPeer));

			this.scriptContext = scriptContext;
			this.jsPeer = jsPeer;
		}

		public string CssClass => jsPeer.HasProperty ("cssClass") ? jsPeer.cssClass : null;
		public bool IsEnabled => jsPeer.HasProperty ("isEnabled") ? jsPeer.isEnabled : true;
		public bool CanExpand => jsPeer.HasProperty ("canExpand") ? jsPeer.canExpand : false;

		public RenderState RenderState { get; private set; }

		public void Bind (RenderState renderState)
		{
			if (renderState.Source != null && !(renderState.Source is WrappedObject))
				throw new ArgumentException ("can only bind to script objects", nameof (renderState));

			RenderState = renderState;
			jsPeer.bind (scriptContext.CreateObject (o => {
				o.source = renderState.Source;
				o.cultureInfo = scriptContext.CreateObject (c => {
					c.name = renderState.CultureInfo.Name;
					c.lcid = renderState.CultureInfo.LCID;
				});
			}));
		}

		public void Collapse ()
		{
			if (jsPeer.HasProperty ("collapse"))
				jsPeer.collapse ();
		}

		public void Expand ()
		{
			if (jsPeer.HasProperty ("expand"))
				jsPeer.expand ();
		}

		public IEnumerable<RendererRepresentation> GetRepresentations ()
		{
			var representations = jsPeer.getRepresentations ();
			if (representations == null)
				yield break;

			int length = representations.length;
			for (var i = 0; i < length; i++) {
				var rep = representations [i];
				if (rep == null)
					continue;

				yield return new JavaScriptRendererRepresentation (rep);
			}
		}

		public void Render (RenderTarget target)
		{
			var rep = target.Representation as JavaScriptRendererRepresentation;
			if (rep == null)
				throw new ArgumentException (
					$"Representation must be a {nameof (JavaScriptRendererRepresentation)}",
					nameof (target));

			jsPeer.render (scriptContext.CreateObject (o => {
				o.representation = rep.JSPeer;
				o.inlineTarget = target.InlineTarget;
				o.expandedTarget = target.ExpandedTarget;
				o.isExpanded = target.IsExpanded;
			}));
		}
	}
}