//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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