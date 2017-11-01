//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Mac.ViewInspector
{
    interface IInspectViewNode
    {
        InspectView InspectView { get; }
        void Focus ();
        void Blur ();
    }
}