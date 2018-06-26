//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        [NonSerialized]
        readonly Exception exception;
        public Exception Exception => exception;

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
            Exception exception,
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
            this.exception = exception;
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
                Exception,
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