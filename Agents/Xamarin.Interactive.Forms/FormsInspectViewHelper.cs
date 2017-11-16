//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Forms
{
    static class FormsInspectViewHelper
    {
        public static ViewVisibility GetViewVisibility (this Page page)
        {
            if (page.Bounds.Width <= 0 && page.Bounds.Height <= 0)
                return ViewVisibility.Collapsed;

            return page.IsVisible ? ViewVisibility.Visible : ViewVisibility.Collapsed;
        }

        public static ViewVisibility GetViewVisibility (this VisualElement velement)
        {
            if (velement.Bounds.Width <= 0 && velement.Bounds.Height <= 0)
                return ViewVisibility.Collapsed;

            return velement.IsVisible ? ViewVisibility.Visible : ViewVisibility.Collapsed;
        }

        public static void HandlePageChildren<TInspectView> (
            Page page,
            Func<Page, bool, TInspectView> pageFactory,
            Func<Element, TInspectView> elementFactory,
            Action<TInspectView> subviewCallback)
        {
            var toolbarItems = page.ToolbarItems;

            if (toolbarItems != null && toolbarItems.Count > 0) {
                for (int i = 0; i < toolbarItems.Count; i++)
                    subviewCallback (elementFactory (toolbarItems [i]));
            }

            var children = (page as IPageController)?.InternalChildren;
            if (children != null && children.Count > 0) {
                var pageIsNavigationOrMasterDetail = page is NavigationPage || page is MasterDetailPage;
                for (int i = 0; i < children.Count; i++) {
                    var childPage = children [i] as Page;
                    if (childPage != null)
                        subviewCallback (pageFactory (childPage, pageIsNavigationOrMasterDetail));
                    else
                        subviewCallback (elementFactory (children [i]));
                }
            }
        }

        public static bool TryGetHighlightedView (
            double x,
            double y,
            bool clear,
            Func<VisualElement, IInspectView> inspectViewFactory,
            Action resetHighlightOnView,
            Action<VisualElement> drawHighlightOnView,
            Func<VisualElement, Rectangle> getNativeViewBounds,
            out IInspectView highlightedView)
        {
            highlightedView = null;

            resetHighlightOnView ();

            var formsApp = Application.Current;
            if (formsApp == null)
                return false;

            var topPage = formsApp.MainPage;
            var navigationStack = topPage.Navigation.NavigationStack;
            var hierarchyRoot = navigationStack.Count == 0 ? topPage : navigationStack.Last ();

            var view = GetViewAt (hierarchyRoot, x, y, getNativeViewBounds);

            if (view == null)
                return false;

            if (!clear)
                drawHighlightOnView (view);


            highlightedView = inspectViewFactory (view);

            if (highlightedView != null && clear) {
                var bounds = getNativeViewBounds (view);
                highlightedView.X = bounds.X;
                highlightedView.Y = bounds.Y;
                highlightedView.Width = bounds.Width;
                highlightedView.Height = bounds.Height;
            }

            return highlightedView != null;
        }

        static IList<Element> GetChildren (Element element)
        {
            var pageController = element as IPageController;
            if (pageController != null)
                return pageController.InternalChildren;

            var elementController = element as IElementController;
            if (elementController != null)
                return elementController.LogicalChildren;

            return Array.Empty<Element> ();
        }

        static VisualElement GetViewAt (Element root, double x, double y, Func<VisualElement, Rectangle> getNativeViewBounds)
        {
            var children = GetChildren (root);

            for (int i = children.Count - 1; i >= 0; i--) {
                var child = children [i];

                // If this child is not a visual element, skip it.
                var velement = child as VisualElement;
                if (velement == null)
                    continue;

                var nativeBounds = getNativeViewBounds (velement);
                if (!nativeBounds.Contains (x, y))
                    continue;

                var grandChild = GetViewAt (velement, x, y, getNativeViewBounds);
                if (grandChild != null)
                    return grandChild;

                return velement;
            }

            var rootAsVisual = root as VisualElement;
            if (rootAsVisual != null && getNativeViewBounds (rootAsVisual).Contains (x, y))
                return rootAsVisual;

            return null;
        }

        public static void HandleElementChildren<TInspectView> (
            Element element,
            Func<Element, TInspectView> elementFactory,
            Action<TInspectView> subviewCallback)
        {
            var children = (element as IVisualElementController)?.LogicalChildren;
            if (children != null && children.Count > 0) {
                for (int i = 0; i < children.Count; i++)
                    subviewCallback (elementFactory (children [i]));
            }

            // TODO: Figure out if other elements that are templated or somehow
            //       have other children can have them displayed sanely here.
        }

        public static IInspectView GetInspectView<TInspectView, TRootInspectView, TNativeView> (
            TNativeView view,
            Predicate<TNativeView> isTop,
            Func<Page, Page, TInspectView> containedPageFactory,
            Func<Page, TInspectView> pageFactory,
            Func<TNativeView, TInspectView> elementFactory,
            Func<Exception, TInspectView> emptyFactory,
            Func<TRootInspectView> rootFactory)
            where TInspectView : class, IInspectView
            where TRootInspectView : class, IInspectView
        {
            try {
                if (isTop (view)) {
                    // This is the top view, so we should get the app hierarchy.
                    var formsApp = Application.Current;
                    if (formsApp == null)
                        return emptyFactory (null);

                    var topPage = formsApp.MainPage;
                    var navigationStack = topPage.Navigation.NavigationStack;
                    var modalStack = topPage.Navigation.ModalStack;

                    var hierarchyRoot = navigationStack.Count == 0 ? topPage : navigationStack.Last ();
                    var modalRoot = modalStack.Count == 0 ? null : modalStack.Last ();

                    var fakeRoot = rootFactory ();
                    var roots = new [] { hierarchyRoot, modalRoot };

                    foreach (var root in roots) {
                        if (root == null)
                            continue;

                        if (root.Parent is NavigationPage || root.Parent is MasterDetailPage)
                            fakeRoot.AddSubview (containedPageFactory (
                                (Page)root.Parent,
                                root));
                        else if (root is TabbedPage)
                            fakeRoot.AddSubview (containedPageFactory (
                                root,
                                (root as TabbedPage).CurrentPage));
                        else
                            fakeRoot.AddSubview (pageFactory (root));
                    }

                    return fakeRoot;
                }
                return elementFactory (view);
            } catch (Exception e) {
                Log.Error (nameof (FormsInspectViewHelper), "Error while trying to get view", e);
                return emptyFactory (e);
            }
        }

        public static string GetDescriptionFromElement (
            Element element,
            TypeMap<Func<Element, string>> customConverters = null)
        {
            if (element is Button)
                return (element as Button).Text;
            if (element is Label)
                return (element as Label).Text;
            if (customConverters != null) {
                var conv = customConverters.GetValues (element.GetType ()).FirstOrDefault ();
                if (conv != null)
                    return conv (element);
            }
            return null;
        }

        public static void HandleContainerChildren<TInspectView> (
            Page container,
            Page page,
            Func<Page, TInspectView> pageFactory,
            Func<Element, TInspectView> elementFactory,
            Action<TInspectView> subviewCallback)
        {
            var children = (container as IPageController)?.InternalChildren;
            if (children != null && children.Count > 0) {
                for (int i = 0; i < children.Count; i++) {
                    var child = children [i];
                    if (child is Page) {
                        if (child == page)
                            subviewCallback (pageFactory (child as Page));
                    } else
                        subviewCallback (elementFactory (child));
                }
            }
        }
    }
}
