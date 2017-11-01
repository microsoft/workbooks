//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Events;

namespace Xamarin.Interactive.Client
{
    sealed class ClientSessionEvent : IEvent, Telemetry.IDataEvent
    {
        public ClientSession Source { get; }
        public ClientSessionEventKind Kind { get; }
        public DateTime Timestamp { get; }

        object IEvent.Source => Source;

        string Telemetry.IEvent.Key => $"cs.{Kind}";

        public ClientSessionEvent (ClientSession source, ClientSessionEventKind kind)
        {
            Timestamp = DateTime.UtcNow;
            Source = source;
            Kind = kind;
        }

        Task Telemetry.IDataEvent.SerializePropertiesAsync (JsonTextWriter writer)
        {
            if (Source == null)
                return Task.CompletedTask;

            writer.WritePropertyName ("cs.id");
            writer.WriteValue (Source.Id.ToString ());

            writer.WritePropertyName ("kind");
            writer.WriteValue (Source.SessionKind.ToString ());

            var agentIdentity = Source?.Agent?.Identity;
            if (agentIdentity != null) {
                writer.WritePropertyName ("agent");
                writer.WriteValue (agentIdentity.AgentType.ToString ());
            }

            return Task.CompletedTask;
        }
    }
}