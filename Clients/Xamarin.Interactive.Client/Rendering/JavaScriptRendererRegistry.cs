//
// JavaScriptRendererRegistry.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Rendering
{
	sealed class JavaScriptRendererRegistry : RendererRegistry
	{
		const string TAG = nameof (JavaScriptRendererRegistry);

		#pragma warning disable 0414
		readonly ScriptContext scriptContext;
		readonly dynamic registry;
		#pragma warning restore 0414

		public JavaScriptRendererRegistry (HtmlDocument document)
		{
			if (document == null)
				throw new ArgumentNullException (nameof (document));

			scriptContext = document.Context;
			registry = scriptContext.GlobalObject.xiexports.RendererRegistry;
		}

		public override IEnumerable<IRenderer> GetRenderers (object source)
		{
			foreach (var renderer in base.GetRenderers (source))
				yield return renderer;

			if (registry == null || (source != null && !(source is WrappedObject)))
				yield break;

			var renderers = registry.getRenderers (source);
			if (renderers == null)
				yield break;

			int length = renderers.length;
			for (var i = 0; i < length; i++) {
				var jsRenderer = renderers [i];
				if (jsRenderer == null)
					continue;

				JavaScriptRenderer renderer = null;

				try {
					renderer = new JavaScriptRenderer (scriptContext, jsRenderer);
				} catch (Exception e) {
					Log.Error (TAG, "Unable to create JavaScriptRenderer", e);
				}

				if (renderer != null)
					yield return renderer;
			}
		}
	}
}