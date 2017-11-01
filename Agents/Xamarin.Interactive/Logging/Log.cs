//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Xamarin.Interactive.Logging
{
    public static class Log
    {
        static readonly DateTime startTime = DateTime.UtcNow;
        static ILogProvider provider;

        public static bool IsInitialized => provider != null;

        internal static void Initialize (ILogProvider logProvider)
        {
            if (logProvider == null)
                throw new ArgumentNullException (nameof (logProvider));

            if (provider != null)
                throw new InvalidOperationException ("already initialized");

            provider = logProvider;
        }

        internal static LogLevel GetLogLevel ()
            => provider.LogLevel;

        internal static void SetLogLevel (LogLevel logLevel)
            => provider.LogLevel = logLevel;

        internal static event EventHandler<LogEntry> EntryAdded {
            add { provider.EntryAdded += value; }
            remove { provider.EntryAdded -= value; }
        }

        internal static LogEntry [] GetEntries ()
            => provider.GetEntries ();

        internal static void Commit (LogEntry logEntry)
            => provider.Commit (logEntry);

        public static void Commit (
            LogLevel level,
            string tag,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                level,
                LogFlags.None,
                tag,
                message,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        internal static void Commit (
            LogLevel level,
            LogFlags flags,
            string tag,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var time = DateTime.UtcNow;
            provider.Commit (new LogEntry (
                null,
                time,
                time - startTime,
                level,
                flags,
                tag,
                message,
                callerMemberName,
                callerFilePath,
                callerLineNumber));
        }

        public static void Verbose (
            string tag,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Verbose,
                LogFlags.None,
                tag,
                message,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Debug (
            string tag,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Debug,
                LogFlags.None,
                tag,
                message,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Info (
            string tag,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Info,
                LogFlags.None,
                tag,
                message,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Warning (
            string tag,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Warning,
                LogFlags.None,
                tag,
                message,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Warning (
            string tag,
            Exception exception,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Warning,
                LogFlags.None,
                tag,
                $"exception: {exception}",
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Warning (
            string tag,
            string message,
            Exception exception,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Warning,
                LogFlags.None,
                tag,
                $"{message}: {exception}",
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Error (
            string tag,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Error,
                LogFlags.None,
                tag,
                message,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Error (
            string tag,
            Exception exception,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Error,
                LogFlags.None,
                tag,
                $"exception: {exception}",
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Error (
            string tag,
            string message,
            Exception exception,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Error,
                LogFlags.None,
                tag,
                $"{message}: {exception}",
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Critical (
            string tag,
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Critical,
                LogFlags.None,
                tag,
                message,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Critical (
            string tag,
            Exception exception,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Critical,
                LogFlags.None,
                tag,
                $"exception: {exception}",
                callerMemberName,
                callerFilePath,
                callerLineNumber);

        public static void Critical (
            string tag,
            string message,
            Exception exception,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
            => Commit (
                LogLevel.Critical,
                LogFlags.None,
                tag,
                $"{message}: {exception}",
                callerMemberName,
                callerFilePath,
                callerLineNumber);
    }
}