//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive
{
    interface ISimplyObservable<T> : IObservable<T>
    {
        IDisposable Subscribe (Action<T> nextHandler);
    }
}