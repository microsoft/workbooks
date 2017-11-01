//
// HtmlRendererBase.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Rendering.Renderers
{
    abstract class HtmlRendererBase : IRenderer
    {
        protected HtmlRendererBase ()
        {
        }

        public abstract string CssClass { get; }

        #region IRenderer

        public virtual bool IsEnabled => true;
        public virtual bool CanExpand => false;

        public RenderState RenderState { get; private set; }

        public void Bind (RenderState renderState)
        {
            RenderState = renderState;
            HandleBind ();
        }

        protected virtual void HandleBind ()
        {
        }

        public void Expand () => HandleExpand ();

        protected virtual void HandleExpand ()
        {
        }

        public void Collapse () => HandleCollapse ();

        protected virtual void HandleCollapse ()
        {
        }

        public IEnumerable<RendererRepresentation> GetRepresentations () => HandleGetRepresentations ();

        protected abstract IEnumerable<RendererRepresentation> HandleGetRepresentations ();

        public void Render (RenderTarget target) => HandleRender (target);

        protected abstract void HandleRender (RenderTarget target);

        #endregion

        #region Subclass Utilities

        protected HtmlDocument Document => RenderState.Context.Document;
        protected RendererContext Context => RenderState.Context;

        protected HtmlElement CreateRenderedTypeNameElement (RepresentedType type)
        {
            return Document.CreateElement ("code", innerHtml: TypeHelper.GetCSharpTypeName (type.Name));
        }

        protected HtmlElement CreateHeaderElement (object obj, string detailsText = null)
        {
            if (obj == null)
                throw new ArgumentNullException (nameof(obj));

            return CreateHeaderElement (RepresentedType.Lookup (obj.GetType ()), detailsText);
        }

        protected HtmlElement CreateHeaderElement (RepresentedType type, string detailsText = null)
        {
            if (type == null)
                throw new ArgumentNullException (nameof(type));

            var headerElem = Document.CreateElement ("header");
            headerElem.AppendChild (CreateRenderedTypeNameElement (type));

            if (detailsText != null) {
                headerElem.AppendChild (
                    Document.CreateElement ("span",
                        @class: "details",
                        innerHtml: detailsText
                    )
                );
            }

            return headerElem;
        }

        protected HtmlElement CreateToStringRepresentationElement (string format, string result)
        {
            var preElem = Document.CreateElement ("pre", @class: "to-string-representation");
            var spanElem = Document.CreateElement ("span", @class: "representation-name");
            if (format == null)
                spanElem.InnerHTML = "ToString()";
            else
                spanElem.InnerHTML = String.Format (
                    "ToString(<span class=\"csharp-string\">&quot;{0}&quot;</span>)",
                    format);

            preElem.AppendChild (spanElem);
            preElem.AppendChild (Document.CreateTextNode (result));
            return preElem;
        }

        protected bool WindowHasSelection ()
        {
            return !String.IsNullOrEmpty (Document.Context.GlobalObject.window.getSelection ().toString ());
        }

        #endregion
    }
}