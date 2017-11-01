//
// IInspectViewNode.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

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