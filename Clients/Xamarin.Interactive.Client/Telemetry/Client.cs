//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

using Microsoft.DotNet.Cli.Telemetry;

using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.SystemInformation;
using Xamarin.Interactive.Telemetry.Models;

namespace Xamarin.Interactive.Telemetry
{
    sealed class Client
    {
        const string TAG = nameof (Telemetry);
        const string ProducerNamespace = "workbooks/client/";

        static readonly TimeSpan defaultFlushTimeout = TimeSpan.FromSeconds (5);

        bool enabled;
        int eventsSent;
        TelemetryClient appInsightsClient;
        InMemoryChannel channel;

        public Client (Guid sessionid, HostEnvironment host, UpdaterService updater)
        {
            if (Interactive.Client.CommandLineTool.TestDriver.ShouldRun)
                return;

            PreferenceStore.Default.Subscribe (ObservePreferenceChange);

            try {
                if (!Prefs.Telemetry.Enabled.GetValue ()) {
                    Log.Info (TAG, "Telemetry is disabled");
                    return;
                }

                // InMemoryChannel is the default channel, but we set it up manually here so we can tweak several
                // default settings that are undesirable for desktop apps.
                channel = new InMemoryChannel {
                    // Defaults to 30s, but since we are changing buffer.Capacity to 1, we can make this infinite and
                    // avoid pointlessly waking up InMemoryTransmitter's Runner.
                    SendingInterval = Timeout.InfiniteTimeSpan,
                };

                // There is no reasonable public API for changing the buffer capacity at this time.
                // You can achieve it by turning on DeveloperMode, but that has other consequences.
                // So we reflect.
                //
                // The default Capacity is 500, which is far too large for us (and perhaps most non-server apps).
                // We want to avoid having to perform a blocking Flush call on the UI thread, and since our events
                // are currently few and far between, we set Capacity to 1 to essentially get auto-flush.
                var channelBuffer = typeof (InMemoryChannel)
                    .GetField ("buffer", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue (channel);
                channelBuffer
                    .GetType ()
                    .GetProperty ("Capacity", BindingFlags.Public | BindingFlags.Instance)
                    .SetValue (channelBuffer, 1);

                var config = new TelemetryConfiguration ("@TELEMETRY_INSTRUMENTATION_KEY@", channel);

                appInsightsClient = new TelemetryClient (config);

                appInsightsClient.Context.Session.Id = sessionid.ToString ();
                appInsightsClient.Context.Device.OperatingSystem = host.OSName.ToString ();

                // TODO: Make these GlobalProperties when we bump to 2.7.0-beta3 or later
                var globalProperties = appInsightsClient.Context.Properties;
                globalProperties.Add (
                    "Product Version",
                    BuildInfo.VersionString);
                globalProperties.Add (
                    "Build Hash",
                    BuildInfo.Hash);
                globalProperties.Add (
                    "OS Platform",
                    Runtime.CurrentProcessRuntime.OSPlatform.ToString ());
                globalProperties.Add (
                    "OS Architecture",
                    RuntimeInformation.OSArchitecture.ToString ());
                globalProperties.Add (
                    "Process Architecture",
                    Runtime.CurrentProcessRuntime.Architecture.ToString ());
                globalProperties.Add (
                    "Runtime Identifier",
                    Runtime.CurrentProcessRuntime.RuntimeIdentifier);
                globalProperties.Add (
                    "OS Version",
                    host.OSVersion.ToString ());
                globalProperties.Add (
                    "Release Candidate Level",
                    ((byte)BuildInfo.Version.CandidateLevel).ToString ());
                globalProperties.Add (
                    "Release Candidate Level Name",
                    BuildInfo.Version.CandidateLevel.ToString ().ToLowerInvariant ());
                globalProperties.Add (
                    "Machine ID",
                    Sha256Hasher.Hash (MacAddressGetter.GetMacAddress ()));
                globalProperties.Add (
                    "Update Channel", updater.UpdateChannel);

                enabled = true;
            } catch (Exception e) {
                LogErrorWithoutTelemetry (e, "Unable to create AppInsights client for telemetry");
            }
        }

        void ObservePreferenceChange (PreferenceChange change)
        {
            if (change.Key == Prefs.Telemetry.Enabled.Key) {
                enabled = Prefs.Telemetry.Enabled.GetValue ();
                if (!enabled) {
                    Log.Info (TAG, "Telemetry disabled.");
                    appInsightsClient = null;
                    // Not bothering to Dispose channel, because that will trigger a synchronous Flush
                    channel = null;
                } else if (appInsightsClient == null) {
                    Log.Info (TAG, "Telemetry will be enabled after restart.");
                }
            }
        }

        public void Post (Exception exception, LogEntry logEntry = default (LogEntry))
        {
            if (exception == null)
                throw new ArgumentNullException (nameof (exception));

            if (appInsightsClient == null || !enabled)
                return;

            if (logEntry.Exception == null || logEntry.Flags.HasFlag (LogFlags.SkipTelemetry))
                return;

            try {
                var exceptionTelemetry = new ExceptionTelemetry (exception);

                exceptionTelemetry.Properties.Add ("XIExceptionTag", logEntry.Tag);
                exceptionTelemetry.Properties.Add ("XIExceptionMessage", logEntry.Message);
                exceptionTelemetry.Properties.Add ("XIExceptionCallerMemberName", logEntry.CallerMemberName);
                exceptionTelemetry.Properties.Add ("XIExceptionCallerFilePath", logEntry.CallerFilePath);
                exceptionTelemetry.Properties.Add ("XIExceptionCallerLineNumber", logEntry.CallerLineNumber.ToString ());

                appInsightsClient.TrackException (exceptionTelemetry);
            } catch (Exception e) {
                LogErrorWithoutTelemetry (e, "Error tracking exception");
            }
        }

        public void Post (string eventName, IEnumerable<KeyValuePair<string, string>> properties = null)
        {
            if (string.IsNullOrEmpty (eventName))
                throw new ArgumentNullException (nameof (eventName));

            if (appInsightsClient == null || !enabled)
                return;

            try {
                var eventTelemetry = new EventTelemetry (ProducerNamespace + eventName);
                if (properties != null) {
                    foreach (var pair in properties)
                        eventTelemetry.Properties.Add (pair);
                }

                appInsightsClient.TrackEvent (eventTelemetry);
            } catch (Exception e) {
                LogErrorWithoutTelemetry (e);
                return;
            }

            eventsSent++;

            Log.Verbose (TAG, $"({eventsSent}): {eventName}");
        }

        public void Post (ITelemetryEvent evnt)
        {
            if (evnt == null)
                throw new ArgumentNullException (nameof (evnt));

            Post (evnt.GetType ().Name, evnt.GetProperties ());
        }

        /// <summary>
        /// Flush queued telemetry events synchronously.
        /// </summary>
        /// <param name="timeout">Defaults to 5 seconds.</param>
        public void BlockingFlush (Optional<TimeSpan> timeout = default (Optional<TimeSpan>))
        {
            if (channel == null || !enabled)
                return;

            var flushTimeout = timeout.GetValueOrDefault (defaultFlushTimeout);

            // Flush can block indefinitely, despite the timeout. This has been
            // observed when network connectivity is lost during an app run.
            // Run Flush defensively on a background thread to avoid this.
            Task.Run (() => channel?.Flush (flushTimeout)).Wait (flushTimeout);
        }

        void LogErrorWithoutTelemetry (Exception e, string message = null)
        {
            message = message ?? "exception";
            Log.Commit (
                LogLevel.Error,
                LogFlags.SkipTelemetry,
                TAG,
                $"{message}: {e}",
                e);
        }
    }
}