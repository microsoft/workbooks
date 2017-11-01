//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Telemetry.Events
{
    sealed class Error : Event
    {
        public string Kind { get; }
        public string Tag { get; }
        public string Message { get; }
        public string CallerName { get; }
        public string CallerFile { get; }
        public int CallerLine { get; }

        public Error (LogEntry logEntry) : base ("error.log")
        {
            Kind = logEntry.Level.ToString ();
            Tag = logEntry.Tag;
            CallerName = logEntry.CallerMemberName;
            CallerFile = logEntry.CallerFilePath;
            CallerLine = logEntry.CallerLineNumber;
        }

        public Error (UserPresentableException upe) : base ("error.upe")
        {
            CallerName = upe.CallerMemberName;
            CallerFile = upe.CallerFilePath;
            CallerLine = upe.CallerLineNumber;
        }

        protected override Task SerializePropertiesAsync (JsonTextWriter writer)
        {
            writer.WritePropertyName ("kind");
            writer.WriteValue (Kind);

            writer.WritePropertyName ("tag");
            writer.WriteValue (Tag);

            // NOTE: "Message" is intentionally not serialized as it could contain PII

            writer.WritePropertyName ("caller");
            writer.WriteStartObject ();
            {
                writer.WritePropertyName ("name");
                writer.WriteValue (CallerName);

                writer.WritePropertyName ("file");
                writer.WriteValue (CallerFile);

                if (CallerLine > 0) {
                    writer.WritePropertyName ("line");
                    writer.WriteValue (CallerLine);
                }
            }
            writer.WriteEndObject ();

            return Task.CompletedTask;
        }
    }
}