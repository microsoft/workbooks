//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Representations
{
    public interface IRepresentationManager
    {
        void AddProvider<TRepresentationProvider> ()
            where TRepresentationProvider : RepresentationProvider, new ();
        void AddProvider (string typeName, Func<object, object> handler);
        void AddProvider<T> (Func<T, object> handler);
    }
}