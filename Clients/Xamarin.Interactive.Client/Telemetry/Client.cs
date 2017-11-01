//
// Client.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Telemetry
{
    sealed class Client
    {
        const string TAG = nameof (Telemetry);
        const string TelemetryApiUrl = "@TELEMETRY_API_URL@";

        readonly string sessionId = Guid.NewGuid ().ToString ();

        bool enabled;
        HttpClient httpClient;
        int eventsSent;

        public Client ()
        {
            if (Interactive.Client.CommandLineTool.TestDriver.ShouldRun)
                return;

            PreferenceStore.Default.Subscribe (ObservePreferenceChange);

            try {
                if (!Prefs.Telemetry.Enabled.GetValue () ||
                    !Uri.TryCreate (TelemetryApiUrl, UriKind.Absolute, out var telemetryApiUri)) {
                    Log.Info (TAG, "Telemetry is disabled");
                    return;
                }

                enabled = true;

                httpClient = new HttpClient {
                    BaseAddress = telemetryApiUri
                };

                httpClient.DefaultRequestHeaders.UserAgent.Add (
                    new ProductInfoHeaderValue (
                        "XamarinInteractiveTelemetry",
                        BuildInfo.VersionString));

                Log.Info (TAG, $"Telemetry Session ID: {sessionId}");
            } catch (Exception e) {
                Log.Error (TAG, "Unable to create HttpClient for telemetry", e);
            }
        }

        void DisableTelemetry ()
        {
            // when we receive a 403, we've shut down the DocumentDB service
            // and have switched over to VS Telemetry in a newer release,
            // so disable telemetry now.
            try {
                httpClient = null;
                enabled = false;
            } catch (Exception e) {
                Log.Error (TAG, e);
            }
        }

        void ObservePreferenceChange (PreferenceChange change)
        {
            if (change.Key == Prefs.Telemetry.Enabled.Key) {
                enabled = Prefs.Telemetry.Enabled.GetValue ();
                if (!enabled) {
                    Log.Info (TAG, "Telemetry disabled.");
                    httpClient = null;
                } else if (httpClient == null) {
                    Log.Info (TAG, "Telemetry will be enabled after restart.");
                }
            }
        }

        public void Post (IDataEvent evnt)
        {
            if (httpClient == null || !enabled)
                return;

            PostEventAsync (evnt).Forget ();
        }

        async Task PostEventAsync (IDataEvent evnt)
        {
#pragma warning disable 0168
            for (int i = 0; i < 10; i++) {
                try {
                    await PostEventOnceAsync (evnt);
                    return;
                } catch (SerializationException e) {
                    Log.Error (TAG, e);
                    return;
                } catch (Exception e) {
#if DEBUG
					Log.Error (TAG, $"attempt {i} failed", e);
#endif
                }

                await Task.Delay (TimeSpan.FromMilliseconds (Math.Pow (2, i) * 100));
#pragma warning restore 0168
            }
        }

        async Task PostEventOnceAsync (IDataEvent evnt)
        {
            if (BuildInfo.IsLocalDebugBuild) {
                eventsSent++;
                Log.Verbose (TAG, $"(not-sent: {eventsSent}): {evnt}");
                return;
            }

            var response = await httpClient.PostAsync (
                "addlog",
                new EventObjectStreamContent (this, evnt));

            if (response.StatusCode == HttpStatusCode.Forbidden) {
                Log.Info (TAG, "Disabling telemetry at service's direction (403).");
                DisableTelemetry ();
                return;
            }

            response.EnsureSuccessStatusCode ();

            eventsSent++;

            Log.Verbose (TAG, $"({eventsSent}): {evnt}");
        }

        sealed class EventObjectStreamContent : HttpContent
        {
            Client client;
            IDataEvent evnt;

            static readonly DateTime unixEpoch = new DateTime (
                1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            public EventObjectStreamContent (Client client, IDataEvent evnt)
            {
                if (evnt == null)
                    throw new ArgumentNullException (nameof (evnt));

                this.client = client;
                this.evnt = evnt;

                EnsureHeaders ();
            }

            void EnsureHeaders ()
            {
                // Mono's HttpContent seems to re-allocate 'Headers' some time after
                // the constructor finishes, clobbering anything configured during
                // the constructor. However, it seems to write them on the first call
                // to Stream.Write for the Stream passed to SerializeToStreamAsync,
                // so the fix on Mono is to configure Headers before writing to the
                // stream. This however is not the behavior of .NET, which requires
                // Headers to be configured in the constructor (it likely writes them
                // explicitly to the stream before calling SerializeToStreamAsync,
                // and not as an implementation of the Stream itself. So do both.
                Headers.ContentType = new MediaTypeHeaderValue ("application/json");
            }

            protected override async Task SerializeToStreamAsync (Stream stream, TransportContext context)
            {
                EnsureHeaders ();

                var streamWriter = new StreamWriter (stream);
                var writer = new JsonTextWriter (streamWriter) {
                    Formatting = Formatting.Indented
                };

                try {
                    writer.WriteStartObject ();

                    writer.WritePropertyName ("sessionId");
                    writer.WriteValue (client.sessionId);

                    writer.WritePropertyName ("eventKey");
                    writer.WriteValue (evnt.Key);

                    writer.WritePropertyName ("eventTime");
                    writer.WriteValue ((evnt.Timestamp - unixEpoch).TotalSeconds);

                    await evnt.SerializePropertiesAsync (writer).ConfigureAwait (false);

                    writer.WriteEndObject ();
                } catch (Exception e) {
                    throw new SerializationException ("bad use of JsonTextWriter / JsonSerializer", e);
                }

                writer.Flush ();
            }

            protected override bool TryComputeLength (out long length)
            {
                length = -1;
                return false;
            }
        }
    }
}