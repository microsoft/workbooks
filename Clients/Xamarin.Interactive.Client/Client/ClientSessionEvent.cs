//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Events;

namespace Xamarin.Interactive.Client
{
    sealed class ClientSessionEvent : IEvent
    {
        public ClientSession Source { get; }
        public ClientSessionEventKind Kind { get; }
        public DateTime Timestamp { get; }

        object IEvent.Source => Source;

        public ClientSessionEvent (ClientSession source, ClientSessionEventKind kind)
        {
            Timestamp = DateTime.UtcNow;
            Source = source;
            Kind = kind;
        }
    }
}