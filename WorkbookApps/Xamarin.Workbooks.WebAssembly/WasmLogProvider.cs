//
// Authors:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

using Xamarin.Interactive.Json;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Serialization;

using WebAssembly;

namespace Xamarin.Workbooks.WebAssembly
{
    sealed class WasmLogProvider : ILogProvider
    {
        public LogLevel LogLevel { get; set; }

        #pragma warning disable 0067
        public event EventHandler<LogEntry> EntryAdded;
        #pragma warning restore 0067

        public WasmLogProvider (LogLevel level)
            => LogLevel = level;

        public void Commit (LogEntry entry)
        {
            global::WebAssembly.Runtime.InvokeJSRaw<string, object> (
                out var _,
                "window.Module.logMessage",
                JsonConvert.SerializeObject (
                    new { entry =  entry, toString = entry.ToString () },
                    InteractiveJsonSerializerSettings.SharedInstance));
        }

        public LogEntry [] GetEntries ()
            => Log.GetEntries ();
    }
}