//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace Xamarin.Interactive.Protocol
{
    sealed class MessageChannel
    {
        static readonly TimeSpan maximumSilenceInterval = TimeSpan.FromSeconds (5);

        [Serializable]
        public sealed class Ping : IXipResponseMessage
        {
            public Guid RequestId { get; }

            public Ping (Guid requestId)
                => RequestId = requestId;
        }

        readonly BlockingCollection<object> queue = new BlockingCollection<object> ();

        public void Push (object message)
            => queue.Add (message);

        public void Pump (Guid requestId, Action<object> messageWriter)
        {
            if (messageWriter == null)
                throw new ArgumentNullException (nameof (messageWriter));

            var ping = new Ping (requestId);

            while (true) {
                if (!queue.TryTake (out var message, maximumSilenceInterval))
                    message = ping;

                messageWriter (message);
            }
        }
    }
}