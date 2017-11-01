//
// IViewHierarchyHandler.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Inspection
{
	interface IViewHierarchyHandler
	{
		bool TryGetHighlightedView (double x, double y, bool clear, out IInspectView highlightedView);
		bool TryGetRepresentedView (object view, bool withSubviews, out IInspectView representedView);
	}
}