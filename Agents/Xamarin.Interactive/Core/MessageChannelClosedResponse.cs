//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Protocol;

namespace Xamarin.Interactive.Core
{
    sealed class MessageChannelClosedResponse : Exception, IXipResponseMessage
    {
        public Guid RequestId { get; }

        public MessageChannelClosedResponse (Guid requestId)
            => RequestId = requestId;
    }
}