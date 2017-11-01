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
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Telemetry.Events
{
    sealed class AppSessionStart : Event
    {
        const string TAG = nameof (AppSessionStart);

        readonly string userId;

        public AppSessionStart () : base ("app.sessionStart")
        {
            MainThread.Ensure ();

            // explicitly get and set in the case where it was not persisted
            // and so we were returned the default Guid.NewGuid. save it.
            userId = Prefs.Telemetry.UserGuid.GetValue ();
            Prefs.Telemetry.UserGuid.SetValue (userId);

            // force this object to init on the main thread so that we can read
            // the update channel preference
            Objects.Application.Initialize ();
        }

        protected override async Task SerializePropertiesAsync (JsonTextWriter writer)
        {
            var stopwatch = new Stopwatch ();
            stopwatch.Start ();

            writer.WritePropertyName ("userId");
            writer.WriteValue (userId);

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