//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Telemetry.Events
{
    sealed class AppSessionStart : Event
    {
        const string TAG = nameof (AppSessionStart);

        public AppSessionStart () : base ("app.sessionStart")
        {
            MainThread.Ensure ();

            PreferenceStore.Default.Remove ("telemetry.userGuid");

            // force this object to init on the main thread so that we can read
            // the update channel preference
            Objects.Application.Initialize ();
        }

        protected override async Task SerializePropertiesAsync (JsonTextWriter writer)
        {
            var stopwatch = new Stopwatch ();
            stopwatch.Start ();

            writer.WritePropertyName ("app");
            Objects.Application.Instance.Serialize (writer);

            writer.WritePropertyName ("env");

            await ClientApp
                .SharedInstance
                .Host
                .WriteJsonAsync (writer)
                .ConfigureAwait (false);

            stopwatch.Stop ();

            Log.Verbose (TAG, $"Serialized in {stopwatch.Elapsed}");
        }
    }
}