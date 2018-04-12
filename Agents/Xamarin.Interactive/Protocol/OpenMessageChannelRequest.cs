// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class OpenMessageChannelRequest : IXipRequestMessage<Agent>
    {
        public Guid MessageId { get; }

        readonly int requestingProcessId;

        [JsonConstructor]
        public OpenMessageChannelRequest (
            Guid messageId,
            int requestingProcessId)
        {
            MessageId = messageId;
            this.requestingProcessId = requestingProcessId;
        }

        public void Handle (Agent agent, Action<object> responseWriter)
        {
            var agentIdentity = agent.Identity;
            var logEntryOwnerId = String.Format ("{0} ({1}:{2})",
                agentIdentity.ApplicationName,
                agentIdentity.Host,
                agentIdentity.Port);

            EventHandler<LogEntry> enqueueLogEntry = (sender, logEntry) =>
                agent.MessageChannel.Push (logEntry.WithOwnerId (logEntryOwnerId));

            var sendLogMessages = requestingProcessId != Process.GetCurrentProcess ().Id;

            if (sendLogMessages) {
                foreach (var logEntry in Log.GetEntries ())
                    enqueueLogEntry (null, logEntry);

                Log.EntryAdded += enqueueLogEntry;
            }

            try {
                agent.MessageChannel.Pump (MessageId, responseWriter);
            } finally {
                if (sendLogMessages)
                    Log.EntryAdded -= enqueueLogEntry;
            }
        }
    }
}