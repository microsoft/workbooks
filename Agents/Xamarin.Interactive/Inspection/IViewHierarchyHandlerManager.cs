//
// IViewHierarchyHandlerManager.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Xamarin.Interactive.Inspection
{
    interface IViewHierarchyHandlerManager
    {
        IReadOnlyList<string> AvailableHierarchyKinds { get; }
        void AddViewHierarchyHandler (string hierarchyKind, IViewHierarchyHandler handler);
    }
}