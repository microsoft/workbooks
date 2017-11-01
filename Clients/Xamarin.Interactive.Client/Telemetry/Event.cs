//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Telemetry
{
    class Event : IDataEvent
    {
        public string Key { get; }
        public DateTime Timestamp { get; }

        public Event (string key) : this (key, DateTime.UtcNow)
        {
        }

        public Event (string key, DateTime timestamp)
        {
            Key = key ?? throw new ArgumentNullException (nameof (key));
            Timestamp = timestamp;
        }

        Task IDataEvent.SerializePropertiesAsync (JsonTextWriter writer)
            => SerializePropertiesAsync (writer);

        protected virtual Task SerializePropertiesAsync (JsonTextWriter writer)
            => Task.CompletedTask;

        public override string ToString ()
            => $"{Key} @ {Timestamp}";
    }
}