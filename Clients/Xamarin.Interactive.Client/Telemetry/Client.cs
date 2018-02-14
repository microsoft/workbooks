//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Telemetry.Models;

namespace Xamarin.Interactive.Telemetry
{
    sealed class Client
    {
        const string TAG = nameof (Telemetry);
        const string DefaultTelemetryEndpoint = "@TELEMETRY_API_URL@";

        bool enabled;
        HttpClient httpClient;
        int eventsSent;

        public Client ()
        {
            if (Interactive.Client.CommandLineTool.TestDriver.ShouldRun)
                return;

            PreferenceStore.Default.Subscribe (ObservePreferenceChange);

            try {
                var telemetryEndpoint = DefaultTelemetryEndpoint;

                if (BuildInfo.IsLocalDebugBuild) {
                    try {
                        var serverProc = SystemInformation
                            .SystemProcessInfo
                            .GetAllProcesses ()
                            .FirstOrDefault (proc =>
                                 proc.ExecPath.EndsWith (
                                     "dotnet",
                                     StringComparison.Ordinal) &&
                                 proc.Arguments.Any (a => a.EndsWith (
                                     "/Xamarin.Interactive.Telemetry.Server.dll",
                                     StringComparison.Ordinal)));

                        if (serverProc != null)
                            // FIXME: server process to provide endpoint somehow
                            telemetryEndpoint = "http://localhost:5000/api/";
                    } catch {
                    }
                }

                if (!Prefs.Telemetry.Enabled.GetValue () ||
                    !Uri.TryCreate (telemetryEndpoint, UriKind.Absolute, out var telemetryApiUri)) {
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

        public void Post (ITelemetryEvent evnt)
        {
            if (httpClient == null || !enabled)
                return;

            PostEventAsync (evnt).Forget ();
        }

        async Task PostEventAsync (ITelemetryEvent evnt)
        {
#pragma warning disable 0168
            for (int i = 0; i < 10; i++) {
                try {
                    await PostEventOnceAsync (evnt);
                    return;
                } catch (JsonSerializationException e) {
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

        async Task PostEventOnceAsync (ITelemetryEvent evnt)
        {
            var response = await httpClient.PostAsync (
                "logEvent",
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
            sealed class ModelBinder : SerializationBinder
            {
                public override void BindToName (Type serializedType, out string assemblyName, out string typeName)
                {
                    assemblyName = null;
                    typeName = serializedType.Name;
                }

                public override Type BindToType (string assemblyName, string typeName)
                    => throw new NotImplementedException ();
            }

            static readonly JsonSerializer serializer = new JsonSerializer {
                Binder = new ModelBinder (),
                TypeNameHandling = TypeNameHandling.Objects,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            Client client;
            ITelemetryEvent evnt;

            public EventObjectStreamContent (Client client, ITelemetryEvent evnt)
            {
                this.client = client
                    ?? throw new ArgumentNullException (nameof (client));

                this.evnt = evnt
                    ?? throw new ArgumentNullException (nameof (evnt));

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

            protected override Task SerializeToStreamAsync (Stream stream, TransportContext context)
            {
                EnsureHeaders ();

                var writer = new StreamWriter (stream);

                try {
                    serializer.Serialize (writer, evnt);
                } catch (Exception e) {
                    throw new JsonSerializationException ("bad use of JsonTextWriter / JsonSerializer", e);
                }

                writer.Flush ();

                return Task.CompletedTask;
            }

            protected override bool TryComputeLength (out long length)
            {
                length = -1;
                return false;
            }
        }
    }

    sealed class JsonSerializationException : Exception
    {
        public JsonSerializationException (string message, Exception innerException)
            : base (message, innerException)
        {
        }
    }
}