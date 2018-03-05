//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Representations
{
    public interface IInteractiveObject : IRepresentationObject
    {
        long RepresentedObjectHandle { get; }
        long Handle { get; set; }
        void Initialize ();
        void Reset ();
        IInteractiveObject Interact (object message);
    }
}