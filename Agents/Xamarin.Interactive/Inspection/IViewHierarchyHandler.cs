//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Inspection
{
    interface IViewHierarchyHandler
    {
        bool TryGetHighlightedView (double x, double y, bool clear, out IInspectView highlightedView);
        bool TryGetRepresentedView (object view, bool withSubviews, out IInspectView representedView);
    }
}