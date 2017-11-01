//
// ILogProvider.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Logging
{
    interface ILogProvider
    {
        event EventHandler<LogEntry> EntryAdded;
        LogLevel LogLevel { get; set; }
        void Commit (LogEntry entry);
        LogEntry [] GetEntries ();
    }
}