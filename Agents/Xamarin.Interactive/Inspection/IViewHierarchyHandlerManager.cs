//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Xamarin.Interactive.Inspection
{
    interface IViewHierarchyHandlerManager
    {
        IReadOnlyList<string> AvailableHierarchyKinds { get; }
        void AddViewHierarchyHandler (string hierarchyKind, IViewHierarchyHandler handler);
    }
}