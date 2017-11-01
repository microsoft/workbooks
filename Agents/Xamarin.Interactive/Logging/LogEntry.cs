//
// LogEntry.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Logging
{
	[Serializable]
	public struct LogEntry
	{
		internal string OwnerId { get; }
		public DateTime Time { get; }
		public TimeSpan RelativeTime { get; }
		public LogLevel Level { get; }
		internal LogFlags Flags { get; }
		public string Tag { get; }
		public string Message { get; }
		public string CallerMemberName { get; }
		public string CallerFilePath { get; }
		public int CallerLineNumber { get; }

		internal LogEntry (
			string ownerId,
			DateTime time,
			TimeSpan relativeTime,
			LogLevel level,
			LogFlags flags,
			string tag,
			string message,
			string callerMemberName,
			string callerFilePath,
			int callerLineNumber)
		{
			OwnerId = ownerId;
			Time = time;
			RelativeTime = relativeTime;
			Level = level;
			Flags = flags;
			Tag = tag;
			Message = message;
			CallerMemberName = callerMemberName;
			CallerFilePath = callerFilePath;
			CallerLineNumber = callerLineNumber;
		}

		internal LogEntry WithOwnerId (string ownerId)
			=> new LogEntry (
				ownerId,
				Time,
				RelativeTime,
				Level,
				Flags,
				Tag,
				Message,
				CallerMemberName,
				CallerFilePath,
				CallerLineNumber);

		public override string ToString ()
		{
			if (Flags.HasFlag (LogFlags.NoFlair))
				return Message;

			var message = $"[{Level}][{RelativeTime}] {Tag} ({CallerMemberName}): {Message}" +
				$" @ {CallerFilePath}:{CallerLineNumber}";

			if (!String.IsNullOrEmpty (OwnerId))
				message = OwnerId + " => " + message;

			return message;
		}
	}
}