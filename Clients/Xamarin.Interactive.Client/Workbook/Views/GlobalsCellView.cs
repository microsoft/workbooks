// Contributed under the MIT License.

using System;
using System.Globalization;
using Xamarin.CrossBrowser;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Rendering;
using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Workbook.Views
{
    sealed class GlobalsCellView : CellView
    {
        HtmlElement resultElem;
        readonly RendererContext rendererContext;

        public GlobalsCellView (
            HtmlDocument document
            , RendererContext rendererContext) : base (
            document,
            "submission csharp")
        {
            //var container = Document.CreateElement ("div", "editor");
            //container.InnerHTML = "HELLO WORLD";
            //System.Threading.SynchronizationContext.Current.Post (o => ((dynamic)container).style.opacity = 1, null);
            //ContentElement.AppendChild(container);
            ContentElement.AppendChild (resultElem = CreateContentContainer ("results"));
            this.rendererContext = rendererContext
                                   ?? throw new ArgumentNullException (nameof (rendererContext));
        }

        static void RemoveElement (ref HtmlElement element)
        {
            element?.ParentElement?.RemoveChild (element);
            element = null;
        }

        public void Reset ()
        {
            RemoveElement (ref resultElem);
        }


        public void RenderResult (
            CultureInfo cultureInfo,
            SimpleVariable [] results)
        {
            resultElem.RemoveChildren ();

            // would be nice to pass in a single thing here really
            // - so work more like GlobalVars...
            foreach (var result in results) {
                var childElem = CreateContentContainer ("result");
                resultElem.AppendChild (childElem);
                rendererContext.Render (
                    RenderState.Create (result.Value, cultureInfo),
                    childElem);
            }
        }

        public override IEditor Editor => null;
        public override void Focus (bool scrollIntoView = true)
        {
            // empty!
        }
    }
}