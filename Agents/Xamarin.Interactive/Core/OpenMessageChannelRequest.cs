//
// OpenMessageChannelRequest.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Diagnostics;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class OpenMessageChannelRequest : IXipRequestMessage<Agent>
	{
		public Guid MessageId { get; } = Guid.NewGuid ();

		readonly int requestingProcessId;

		public OpenMessageChannelRequest (int requestingProcessId)
		{
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
