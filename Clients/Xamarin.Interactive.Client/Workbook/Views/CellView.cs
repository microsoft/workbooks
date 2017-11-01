//
// CellView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Threading;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Rendering;

namespace Xamarin.Interactive.Workbook.Views
{
    abstract class CellView : ICellView
    {
        public HtmlDocument Document { get; }
        public HtmlElement RootElement { get; }
        public HtmlElement ContentElement { get; }
        public HtmlElement FooterElement { get; }

        public abstract IEditor Editor { get; }

        protected CellView (HtmlDocument document, string cssClass)
        {
            if (document == null)
                throw new ArgumentNullException (nameof(document));

            Document = document;

            RootElement = Document.CreateElement ("article", cssClass);
            ContentElement = Document.CreateElement ("section");
            FooterElement = Document.CreateElement ("footer");

            RootElement.AppendChild (ContentElement);
            RootElement.AppendChild (FooterElement);
        }

        public abstract void Focus (bool scrollIntoView = true);

        protected HtmlElement CreateContentContainer (string cssClass = null)
        {
            var container = Document.CreateElement ("div", cssClass);
            SynchronizationContext.Current.Post (o => ((dynamic)container).style.opacity = 1, null);
            return container;
        }
    }
}