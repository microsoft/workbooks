//
// ISimplyObservable.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive
{
    interface ISimplyObservable<T> : IObservable<T>
    {
        IDisposable Subscribe (Action<T> nextHandler);
    }
}