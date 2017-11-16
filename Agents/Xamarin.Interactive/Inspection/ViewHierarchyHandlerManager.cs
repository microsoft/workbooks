//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Inspection
{
    sealed class ViewHierarchyHandlerManager : IViewHierarchyHandlerManager
    {
        const string TAG = nameof (ViewHierarchyHandlerManager);

        OrderedMapOfList<string, IViewHierarchyHandler> handlers;

        public void AddViewHierarchyHandler (string hierarchyKind, IViewHierarchyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException (nameof (handler));

            MainThread.Ensure ();

            if (handlers == null)
                handlers = new OrderedMapOfList<string, IViewHierarchyHandler> ();

            handlers.Add (hierarchyKind, handler);
        }

        public IReadOnlyList<string> AvailableHierarchyKinds
            => handlers?.Keys ?? Array.Empty<string> ();

        public InspectView HighlightView (double x, double y, bool clear, string hierarchyKind)
        {
            IReadOnlyList<IViewHierarchyHandler> handlersForKind;
            if (!handlers.TryGetValue (hierarchyKind, out handlersForKind))
                return null;

            foreach (var handler in handlersForKind) {
                IInspectView highlightedView;
                if (handler.TryGetHighlightedView (x, y, clear, out highlightedView))
                    return highlightedView as InspectView;
            }

            return null;
        }

        public InspectView GetView (object view, string hierarchyKind, bool withSubviews = true)
        {
            if (view == null || hierarchyKind == null || handlers == null)
                return null;

            IReadOnlyList<IViewHierarchyHandler> handlersForKind;
            if (!handlers.TryGetValue (hierarchyKind, out handlersForKind))
                return null;

            foreach (var handler in handlersForKind) {
                IInspectView representedView;
                if (handler.TryGetRepresentedView (view, withSubviews, out representedView))
                    return representedView as InspectView;
            }

            return null;
        }
    }
}