// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Protocol
{
    interface IXipRequestMessage<T>
    {
        void Handle (T context, Action<object> responseWriter);
    }
}