//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Client.ViewInspector
{
    interface IInspectTree3DNode<T>
    {
        void BuildPrimaryPlane (InspectTreeState state);
        T BuildChild (InspectTreeNode node, InspectTreeState state);
        void Add (T child);
    }
}
