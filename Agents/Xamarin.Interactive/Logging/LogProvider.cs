//
// LogProvider.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Xamarin.Interactive.Logging
{
    sealed class LogProvider : ILogProvider
    {
        static readonly bool isDebuggerAttached = Debugger.IsAttached;
        static readonly TextWriter stderr = Console.Error;

        readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim ();
        readonly List<LogEntry> entries = new List<LogEntry> ();

        public event EventHandler<LogEntry> EntryAdded;

        public LogLevel LogLevel { get; set; }

        public LogProvider (LogLevel logLevel)
        {
            LogLevel = logLevel;
        }

        public void Commit (LogEntry entry)
        {
            var ignoreEntry = entry.Level < LogLevel;

            try {
#if DEBUG
				if (isDebuggerAttached) {
					Debug.WriteLine (entry);
					return;
				}
#endif

                if (ignoreEntry)
                    return;

                stderr.WriteLine (entry);
            } finally {
                if (!ignoreEntry) {
                    rwlock.EnterWriteLock ();
                    try {
                        entries.Add (entry);
                    } finally {
                        rwlock.ExitWriteLock ();
                    }

                    EntryAdded?.Invoke (this, entry);
                }
            }
        }

        public LogEntry [] GetEntries ()
        {
            rwlock.EnterReadLock ();
            try {
                return entries.ToArray ();
            } finally {
                rwlock.ExitReadLock ();
            }
        }
    }
}