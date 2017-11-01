//
// INotifyDisposed.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive
{
    interface INotifyDisposed : IDisposable
    {
        event EventHandler Disposed;
        bool IsDisposed { get; }
    }
}