//
// RootRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Immutable;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Rendering
{
    sealed class RootRenderer
    {
        const string TAG = nameof (RootRenderer);

        const string DataItemIndexAttr = "data-item-index";

        class ItemState
        {
            public string Title;
            public HtmlElement MenuItemElem;
            public HtmlElement InlineRenderedElem;
            public HtmlElement ExpandedRenderedElem;
            public RendererRepresentation RendererRepresentation;
            public IRenderer Renderer;
        }

        readonly RendererContext context;
        readonly RenderState renderState;
        readonly HtmlDocument document;
        ImmutableList<ItemState> itemStates;
        ItemState selectedItemState;

        HtmlElement containerElem;
        HtmlElement expanderElem;
        HtmlElement menuElem;
        HtmlElement menuButtonElem;

        public RootRenderer (RendererContext context, RenderState renderState)
        {
            if (context == null)
                throw new ArgumentNullException (nameof (context));

            if (renderState == null)
                throw new ArgumentNullException (nameof (renderState));

            this.context = context;
            this.renderState = renderState;

            itemStates = ImmutableList<ItemState>.Empty;
            document = context.Document;

            var representedObject = renderState.Source as RepresentedObject;
            if (representedObject != null) {
                foreach (var result in representedObject)
                    AppendSubrenderState (result);
            } else {
                if (renderState.Source != null && !(renderState.Source is JsonPayload))
                    AppendSubrenderState (RepresentationManager.ToJson (renderState.Source));
                AppendSubrenderState (renderState.Source);
            }
        }

        void AppendSubrenderState (object value)
        {
            if (value == null)
                value = (JsonPayload)"null";

            if (value is JsonPayload) {
                try {
                    value = context.Document.Context.GlobalObject.xiexports
                        .DeserializeDotNetObject ((string)(JsonPayload)value);
                } catch (Exception e) {
                    Log.Error (TAG, $"JavaScript threw while deserializing: {value}", e);
                }
            }

            foreach (var renderer in context.Renderers.GetRenderers (value)) {
                renderer.Bind (renderState.With (context, value));
                if (!renderer.IsEnabled)
                    continue;

                foreach (var representation in renderer.GetRepresentations ()) {
                    var rendererTitle = representation.ShortDisplayName;
                    itemStates = itemStates.Add (new ItemState {
                        Title = rendererTitle,
                        Renderer = renderer,
                        RendererRepresentation = representation
                    });
                }
            }

            itemStates = itemStates.Sort ((a, b)
                => a.RendererRepresentation.Order.CompareTo (b.RendererRepresentation.Order));
        }

        public void Render (HtmlElement targetElem)
        {
            containerElem = document.CreateElement ("div", @class: "render-manager-container");

            document.AddEventListener ("click", evnt => {
                if (menuElem == null)
                    return;
                else if (evnt.Target == menuButtonElem)
                    menuElem.AddCssClass ("open");
                else
                    menuElem.RemoveCssClass ("open");
            });

            BuildExpander ();

            if (itemStates.Count > 1)
                BuildMenu ();
            else {
                menuElem = null;
                menuButtonElem = null;
            }

            targetElem.AppendChild (containerElem);

            SelectItem (0);
        }

        void BuildExpander ()
        {
            expanderElem = document.CreateElement ("div");
            expanderElem.ClassName = "xiui-expander-button";
            expanderElem.AddEventListener ("click", evnt => ToggleExpanded ());
        }

        void BuildMenu ()
        {
            menuElem = document.CreateElement ("div");
            menuElem.ClassName = "xiui-dropdown-menu";

            menuButtonElem = document.CreateElement ("div");
            menuButtonElem.ClassName = "button";
            menuElem.AppendChild (menuButtonElem);

            var menuItemsElem = document.CreateElement ("ul");
            menuItemsElem.ClassName = "menu";
            menuElem.AppendChild (menuItemsElem);

            var suppressDisplayName = 0;

            for (int i = 0; i < itemStates.Count; i++) {
                var subrenderState = itemStates [i];

                if (subrenderState.RendererRepresentation.Options.HasFlag (
                    RendererRepresentationOptions.SuppressDisplayNameHint))
                    suppressDisplayName++;

                var menuItemElem = document.CreateElement ("li");
                subrenderState.MenuItemElem = menuItemElem;

                menuItemElem.SetAttribute (DataItemIndexAttr,
                    i.ToString (System.Globalization.CultureInfo.InvariantCulture));
                menuItemElem.AppendChild (document.CreateTextNode (subrenderState.Title));
                menuItemElem.AddEventListener ("click", HandleMenuItemClick);

                menuItemsElem.AppendChild (menuItemElem);
            }

            if (suppressDisplayName == itemStates.Count)
                menuButtonElem.AddCssClass ("no-label");
        }

        void SelectItem (int itemIndex, bool isMenuSelection = false)
        {
            if (itemStates.Count == 0) {
                selectedItemState = null;
                return;
            }

            selectedItemState = itemStates [itemIndex];

            containerElem.RemoveChildren ();

            if (selectedItemState.Renderer == null)
                return;

            LayoutMenu ();
            RenderSelectedItem ();
            LayoutSelectedItem ();

            var options = selectedItemState.RendererRepresentation.Options;
            if (options.HasFlag (RendererRepresentationOptions.ExpandedByDefault) ||
                (options.HasFlag (RendererRepresentationOptions.ExpandedFromMenu) && isMenuSelection))
                ToggleExpanded ();
        }

        void LayoutMenu ()
        {
            if (menuElem == null)
                return;

            for (int i = 0; i < itemStates.Count; i++) {
                if (itemStates [i] == selectedItemState)
                    itemStates [i].MenuItemElem.AddCssClass ("selected");
                else
                    itemStates [i].MenuItemElem.RemoveCssClass ("selected");
            }

            menuButtonElem.RemoveChildren ();

            if (!menuButtonElem.HasCssClass ("no-label"))
                menuButtonElem.AppendChild (document.CreateTextNode (selectedItemState.Title));
        }

        void LayoutSelectedItem ()
        {
            if (expanderElem != null && selectedItemState.Renderer.CanExpand)
                containerElem.AppendChild (expanderElem);

            if (selectedItemState.InlineRenderedElem != null)
                containerElem.AppendChild (selectedItemState.InlineRenderedElem);

            if (menuElem != null)
                containerElem.AppendChild (menuElem);

            if (selectedItemState.ExpandedRenderedElem != null)
                containerElem.AppendChild (selectedItemState.ExpandedRenderedElem);
            else
                return;

            if (selectedItemState.RendererRepresentation.Options.HasFlag (
                RendererRepresentationOptions.ForceExpand)) {
                expanderElem.AddCssClass ("expanded");
                selectedItemState.ExpandedRenderedElem.AddCssClass ("expanded");
            } else {
                expanderElem.RemoveCssClass ("expanded");
                selectedItemState.ExpandedRenderedElem.RemoveCssClass ("expanded");
            }
        }

        void RenderSelectedItem ()
        {
            if (selectedItemState.InlineRenderedElem != null)
                return;

            var cssClass = selectedItemState.Renderer.CssClass;

            var inlineTargetElem = document.CreateElement (
                "figure", @class: "renderer-base renderer-inline");

            if (cssClass != null)
                inlineTargetElem.AddCssClass (cssClass);

            var expandedTargetElem = document.CreateElement (
                "figure", @class: "renderer-base renderer-expandable");

            if (cssClass != null)
                expandedTargetElem.AddCssClass (cssClass);

            containerElem.AppendChild (inlineTargetElem);
            if (expandedTargetElem != null)
                containerElem.AppendChild (expandedTargetElem);

            selectedItemState.Renderer.Render (new RenderTarget (
                selectedItemState.RendererRepresentation,
                inlineTargetElem,
                expandedTargetElem));

            selectedItemState.InlineRenderedElem = inlineTargetElem;
            selectedItemState.ExpandedRenderedElem = expandedTargetElem;
        }

        void ToggleExpanded ()
        {
            expanderElem.ToggleCssClass ("expanded");
            selectedItemState.ExpandedRenderedElem.ToggleCssClass ("expanded");

            if (expanderElem.HasCssClass ("expanded"))
                selectedItemState.Renderer.Expand ();
            else
                selectedItemState.Renderer.Collapse ();
        }

        void HandleMenuItemClick (Event evnt)
        {
            var menuItemElem = (HtmlElement)evnt.Target;
            int itemIndex;
            Int32.TryParse (menuItemElem.GetAttribute (DataItemIndexAttr), out itemIndex);
            SelectItem (itemIndex, isMenuSelection: true);
        }
    }
}