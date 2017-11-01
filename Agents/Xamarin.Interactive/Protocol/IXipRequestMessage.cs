//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Protocol
{
    interface IXipRequestMessage
    {
        Guid MessageId { get; }
    }

    interface IXipRequestMessage<T> : IXipRequestMessage
    {
        void Handle (T context, Action<object> responseWriter);
    }
}