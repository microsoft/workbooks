//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;

using Microsoft.DotNet.Cli.Telemetry;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.SystemInformation;
using Xamarin.Interactive.Telemetry.Models;

namespace Xamarin.Interactive.Telemetry
{
    sealed class Client
    {
        const string TAG = nameof (Telemetry);

        readonly ImmutableDictionary<string, string> commonProperties;

        bool enabled;
        int eventsSent;
        TelemetryClient appInsightsClient;

        public Client (Guid sessionid, HostEnvironment host)
        {
            if (Interactive.Client.CommandLineTool.TestDriver.ShouldRun)
                return;

            PreferenceStore.Default.Subscribe (ObservePreferenceChange);

            try {
                if (!Prefs.Telemetry.Enabled.GetValue ()) {
                    Log.Info (TAG, "Telemetry is disabled");
                    return;
                }

                enabled = true;

                appInsightsClient = new TelemetryClient ();
                appInsightsClient.InstrumentationKey = "@TELEMETRY_INSTRUMENTATION_KEY@";
                appInsightsClient.Context.Session.Id = sessionid.ToString ();
                appInsightsClient.Context.Device.OperatingSystem = host.OSName.ToString ();

                commonProperties = new Dictionary<string, string> {
                    { "Product Version", BuildInfo.VersionString },
                    { "Build Hash", BuildInfo.Hash },
                    { "OS Name", host.OSName.ToString () },
                    { "OS Version", host.OSVersion.ToString () },
                    { "Word Size", IntPtr.Size.ToString () },
                    { "CPU Word Size", (Environment.Is64BitOperatingSystem ? 8 : 4).ToString () },
                    { "Machine ID", Sha256Hasher.Hash (MacAddressGetter.GetMacAddress ())}
                }.ToImmutableDictionary ();

            } catch (Exception e) {
                Log.Error (TAG, "Unable to create AppInsights client for telemetry", e);
            }
        }

        void ObservePreferenceChange (PreferenceChange change)
        {
            if (change.Key == Prefs.Telemetry.Enabled.Key) {
                enabled = Prefs.Telemetry.Enabled.GetValue ();
                if (!enabled) {
                    Log.Info (TAG, "Telemetry disabled.");
                    appInsightsClient = null;
                } else if (appInsightsClient == null) {
                    Log.Info (TAG, "Telemetry will be enabled after restart.");
                }
            }
        }

        public void Post (ITelemetryEvent evnt)
        {
            if (appInsightsClient == null || !enabled)
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

        async Task PostEventOnceAsync (ITelemetryEvent evnt)
        {
            var props = new Dictionary<string, string> (commonProperties);
            foreach (var pair in evnt.GetProperties ())
                props [pair.Key] = pair.Value;

            appInsightsClient.TrackEvent (
                evnt.GetType ().Name,
                properties: props);
            appInsightsClient.Flush ();

            eventsSent++;

            Log.Verbose (TAG, $"({eventsSent}): {evnt}");
        }
    }
}